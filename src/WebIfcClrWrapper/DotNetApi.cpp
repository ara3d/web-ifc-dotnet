/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

#include <string>
#include <algorithm>
#include <vector>
#include <stack>
#include <cstdint>
#include <memory>
#include <spdlog/spdlog.h>
#include "modelmanager/Modelmanager.h"
#include "version.h"
#include <iostream>
#include <fstream>

#include <msclr\marshal_cppstd.h>

// NOTE: this class is a combination of.
// https://github.com/ThatOpen/engine_web-ifc/blob/main/src/ts/web-ifc-api.ts
// https://github.com/ThatOpen/engine_web-ifc/blob/main/src/cpp/web-ifc-wasm.cpp
// It is build based on what the needs of Speckle are:
// https://github.com/specklesystems/speckle-server/blob/main/packages/fileimport-service/ifc/parser_v2.js#L26
// const lines = await this.ifcapi.GetLineIDsWithType(this.modelId, element)

/*
    this.modelId = this.ifcapi.OpenModel(new Uint8Array(data), { USE_FAST_BOOLS: true })
    const allProjectLines = await this.ifcapi.GetLineIDsWithType(this.modelId, WebIFC.IFCPROJECT)
    const rel = await this.ifcapi.GetLine(this.modelId, relation.get(i), false)
    this.ifcapi.GetVertexArray(geometry.GetVertexData(),geometry.GetVertexDataSize())
    this.ifcapi.GetIndexArray(geometry.GetIndexData(), geometry.GetIndexDataSize())
    const allLinesIDs = await this.ifcapi.GetAllLines(this.modelId)
    this.ifcapi.StreamAllMeshes(this.modelId, async (mesh) => { }
*/

using namespace System;
using namespace System::Collections::Generic;

// NOTE: having a static variable is a bad idea. 
namespace  WebIfcClrWrapper
{
    public ref class DotNetApi
    {
    public:
        const bool MT_ENABLED = false;

        webifc::manager::LoaderSettings* settings
            = new webifc::manager::LoaderSettings();

        webifc::manager::ModelManager* manager
            = new webifc::manager::ModelManager(MT_ENABLED);

        int CreateModel() {
            return manager->CreateModel(*settings);
        }

        void CloseAllModels() {
            return manager->CloseAllModels();
        }

        int OpenModel(System::String^ fileName)
        {
            auto modelID = manager->CreateModel(*settings);
            auto loader = manager->GetIfcLoader(modelID);
            std::ifstream ifs;
            std::string unmanaged = msclr::interop::marshal_as<std::string>(fileName);
            ifs.open(unmanaged, std::ifstream::in);
            loader->LoadFile(ifs);
            return modelID;
        }

        int GetModelSize(uint32_t modelID)
        {
            return manager->IsModelOpen(modelID)
                ? manager->GetIfcLoader(modelID)->GetTotalSize()
                : 0;
        }

        void CloseModel(uint32_t modelID)
        {
            return manager->CloseModel(modelID);
        }

        webifc::geometry::IfcFlatMesh GetFlatMesh(uint32_t modelID, uint32_t expressID)
        {
            if (!manager->IsModelOpen(modelID)) return {};
            auto mesh = manager->GetGeometryProcessor(modelID)->GetFlatMesh(expressID);
            for (auto& geom : mesh.geometries)
                manager->GetGeometryProcessor(modelID)->GetGeometry(geom.geometryExpressID).GetVertexData();
            return mesh;
        }

        std::vector<webifc::geometry::IfcFlatMesh> LoadAllGeometry(uint32_t modelID)
        {
            if (!manager->IsModelOpen(modelID)) return std::vector<webifc::geometry::IfcFlatMesh>();
            auto loader = manager->GetIfcLoader(modelID);
            auto geomLoader = manager->GetGeometryProcessor(modelID);
            std::vector<webifc::geometry::IfcFlatMesh> meshes;

            for (auto type : manager->GetSchemaManager().GetIfcElementList())
            {
                auto elements = loader->GetExpressIDsWithType(type);

                if (type == webifc::schema::IFCOPENINGELEMENT || type == webifc::schema::IFCSPACE || type == webifc::schema::IFCOPENINGSTANDARDCASE)
                {
                    continue;
                }

                for (uint32_t i = 0; i < elements.size(); i++)
                {
                    auto mesh = geomLoader->GetFlatMesh(elements[i]);
                    for (auto& geom : mesh.geometries)
                    {
                        auto& flatGeom = geomLoader->GetGeometry(geom.geometryExpressID);
                        flatGeom.GetVertexData();
                    }
                    meshes.push_back(std::move(mesh));
                }
            }

            return meshes;
        }

        webifc::geometry::IfcGeometry GetGeometry(uint32_t modelID, uint32_t expressID)
        {
            return manager->IsModelOpen(modelID)
                ? manager->GetGeometryProcessor(modelID)->GetGeometry(expressID)
                : webifc::geometry::IfcGeometry();
        }

        std::vector<webifc::geometry::IfcCrossSections> GetAllCrossSections(uint32_t modelID, uint8_t dimensions)
        {
            if (!manager->IsModelOpen(modelID)) return std::vector<webifc::geometry::IfcCrossSections>();
            auto geomLoader = manager->GetGeometryProcessor(modelID);

            std::vector<uint32_t> typeList;
            typeList.push_back(webifc::schema::IFCSECTIONEDSOLIDHORIZONTAL);
            typeList.push_back(webifc::schema::IFCSECTIONEDSOLID);
            typeList.push_back(webifc::schema::IFCSECTIONEDSURFACE);

            std::vector<webifc::geometry::IfcCrossSections> crossSections;

            for (auto& type : typeList)
            {
                auto elements = manager->GetIfcLoader(modelID)->GetExpressIDsWithType(type);

                for (size_t i = 0; i < elements.size(); i++)
                {
                    webifc::geometry::IfcCrossSections crossSection;
                    if (dimensions == 2) crossSection = geomLoader->GetLoader().GetCrossSections2D(elements[i]);
                    else crossSection = geomLoader->GetLoader().GetCrossSections3D(elements[i]);
                    crossSections.push_back(crossSection);
                }
            }

            return crossSections;
        }

        std::vector<webifc::geometry::IfcAlignment> GetAllAlignments(uint32_t modelID)
        {
            if (!manager->IsModelOpen(modelID)) return std::vector<webifc::geometry::IfcAlignment>();
            auto geomLoader = manager->GetGeometryProcessor(modelID);
            auto type = webifc::schema::IFCALIGNMENT;

            auto elements = manager->GetIfcLoader(modelID)->GetExpressIDsWithType(type);

            std::vector<webifc::geometry::IfcAlignment> alignments;

            for (size_t i = 0; i < elements.size(); i++)
            {
                webifc::geometry::IfcAlignment alignment = geomLoader->GetLoader().GetAlignment(elements[i]);
                alignment.transform(geomLoader->GetCoordinationMatrix());
                alignments.push_back(alignment);
            }

            return alignments;
        }

        void SetGeometryTransformation(uint32_t modelID, std::array<double, 16> m)
        {
            if (manager->IsModelOpen(modelID))
                manager->GetGeometryProcessor(modelID)->SetTransformation(m);
        }

        std::array<double, 16> GetCoordinationMatrix(uint32_t modelID)
        {
            return  manager->IsModelOpen(modelID)
                ? manager->GetGeometryProcessor(modelID)->GetFlatCoordinationMatrix()
                : std::array<double, 16>();
        }

        std::vector<uint32_t> GetExpressIDs(uint32_t modelID, uint32_t type)
        {
            if (!manager->IsModelOpen(modelID)) return {};
            auto loader = manager->GetIfcLoader(modelID);
            return loader->GetExpressIDsWithType(type);
        }


        bool ValidateExpressID(uint32_t modelID, uint32_t expressId) {
            return manager->IsModelOpen(modelID)
                && manager->GetIfcLoader(modelID)->IsValidExpressID(expressId);
        }

        uint32_t GetNextExpressID(uint32_t modelID, uint32_t expressId) {
            return manager->IsModelOpen(modelID) 
                ? manager->GetIfcLoader(modelID)->GetNextExpressID(expressId) 
                : 0;
        }

        void GetAllLines(uint32_t modelID, List<uint32_t>^ list) {
            auto lines = manager->GetIfcLoader(modelID)->GetAllLines();
            for (auto line : lines)
                list->Add(line);
        }

        std::string GetNameFromTypeCode(uint32_t type) {
            return std::string(manager->GetSchemaManager().IfcTypeCodeToType(type));
        }

        uint32_t GetTypeCodeFromName(std::string typeName) {
            return manager->GetSchemaManager().IfcTypeToTypeCode(typeName);
        }

        bool IsIfcElement(uint32_t type) {
            return manager->GetSchemaManager().IsIfcElement(type);
        }

        void ReadValue(uint32_t modelID, webifc::parsing::IfcTokenType t)
        {
            /*
            // TODO: finish 
            auto loader = manager->GetIfcLoader(modelID);
            switch (t)
            {
            case webifc::parsing::IfcTokenType::STRING:
            {
                //return emscripten::val(loader->GetDecodedStringArgument());
            }
            case webifc::parsing::IfcTokenType::ENUM:
            {
                std::string_view s = loader->GetStringArgument();
                //return emscripten::val(std::string(s));
            }
            case webifc::parsing::IfcTokenType::REAL:
            {
                std::string_view s = loader->GetDoubleArgumentAsString();
                //return emscripten::val(std::string(s));
            }
            case webifc::parsing::IfcTokenType::INTEGER:
            {
                long d = loader->GetIntArgument();
                //return emscripten::val(d);
            }
            case webifc::parsing::IfcTokenType::REF:
            {
                uint32_t ref = loader->GetRefArgument();
                //return emscripten::val(ref);
            }
            default:
                // use undefined to signal val parse issue
                //return emscripten::val::undefined();
            }
            */
        }

        void GetArgs(uint32_t modelID)
        {
            bool inObject = false; 
            bool inList = false;
            
            /*
            auto loader = manager->GetIfcLoader(modelID);
            auto arguments = emscripten::val::array();
            size_t size = 0;
            bool endOfLine = false;
            while (!loader->IsAtEnd() && !endOfLine)
            {
                webifc::parsing::IfcTokenType t = loader->GetTokenType();

                switch (t)
                {
                case webifc::parsing::IfcTokenType::LINE_END:
                {
                    endOfLine = true;
                    break;
                }
                case webifc::parsing::IfcTokenType::EMPTY:
                {
                    arguments.set(size++, emscripten::val::null());
                    break;
                }
                case webifc::parsing::IfcTokenType::SET_BEGIN:
                {
                    arguments.set(size++, GetArgs(modelID, false, true));
                    break;
                }
                case webifc::parsing::IfcTokenType::SET_END:
                {
                    endOfLine = true;
                    break;
                }
                case webifc::parsing::IfcTokenType::LABEL:
                {
                    // read label
                    auto obj = emscripten::val::object();
                    obj.set("type", emscripten::val(static_cast<uint32_t>(webifc::parsing::IfcTokenType::LABEL)));
                    loader->StepBack();
                    auto s = loader->GetStringArgument();
                    auto typeCode = manager->GetSchemaManager().IfcTypeToTypeCode(s);
                    obj.set("typecode", emscripten::val(typeCode));
                    // read set open
                    loader->GetTokenType();
                    obj.set("value", GetArgs(modelID, true));
                    arguments.set(size++, obj);
                    break;
                }
                case webifc::parsing::IfcTokenType::STRING:
                case webifc::parsing::IfcTokenType::ENUM:
                case webifc::parsing::IfcTokenType::REAL:
                case webifc::parsing::IfcTokenType::INTEGER:
                case webifc::parsing::IfcTokenType::REF:
                {
                    loader->StepBack();
                    emscripten::val obj;
                    if (inObject) obj = ReadValue(modelID, t);
                    else {
                        obj = emscripten::val::object();
                        obj.set("type", emscripten::val(static_cast<uint32_t>(t)));
                        obj.set("value", ReadValue(modelID, t));
                    }
                    arguments.set(size++, obj);
                    break;
                }
                default:
                    break;
                }
            }
            if (size == 0 && !inList) return emscripten::val::null();
            if (size == 1 && inObject) return arguments[0];
            return arguments;
            */
        }

        void GetLine(uint32_t modelID, uint32_t expressID)
        {
            auto loader = manager->GetIfcLoader(modelID);
            uint32_t lineType = loader->GetLineType(expressID);
            loader->MoveToArgumentOffset(expressID, 0);

            //GetArgs(modelID);

            // TODO: convert to a struct

            /*
            auto retVal = emscripten::val::object();
            retVal.set(emscripten::val("ID"), expressID);
            retVal.set(emscripten::val("type"), lineType);
            retVal.set(emscripten::val("arguments"), arguments);
            return retVal;
            */
        }

        uint32_t GetLineType(uint32_t modelID, uint32_t expressID) {
            return manager->IsModelOpen(modelID) ? manager->GetIfcLoader(modelID)->GetLineType(expressID) : 0;
        }

        uint32_t GetMaxExpressID(uint32_t modelID) {
            return manager->IsModelOpen(modelID) ? manager->GetIfcLoader(modelID)->GetMaxExpressId() : 0;
        }

        bool IsModelOpen(uint32_t modelID) {
            return manager->IsModelOpen(modelID);
        }


        /**
             * Gets the ifc line data for a given express ID
             * @param modelID Model handle retrieved by OpenModel
             * @param expressID express ID of the line
             * @param flatten recursively flatten the line, default false
             * @param inverse get the inverse properties of the line, default false
             * @param inversePropKey filters out all other properties from a inverse search, for a increase in performance. Default null
             * @returns lineObject
        GetLine(modelID: number, expressID : number)
        {

            let rawLineData = this.GetRawLineData(modelID, expressID);
            let lineData = FromRawLineData[this.modelSchemaList[modelID]][rawLineData.type](rawLineData.arguments);
            lineData.expressID = rawLineData.ID;

            return lineData;
        }
        */
    };
}