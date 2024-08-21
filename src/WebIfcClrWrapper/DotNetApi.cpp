/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. 
 */

#pragma unmanaged
#include <string>
#include <algorithm>
#include <vector>
#include <stack>
#include <cstdint>
#include <memory>
#include <spdlog/spdlog.h>
#include "../engine_web-ifc/src/cpp/modelmanager/ModelManager.h"
#include "../engine_web-ifc/src/cpp/version.h"
#include <iostream>
#include <fstream>
#pragma managed 

#include <msclr\marshal_cppstd.h>

// NOTE: this class is a combination of.
// https://github.com/ThatOpen/engine_web-ifc/blob/main/src/ts/web-ifc-api.ts
// https://github.com/ThatOpen/engine_web-ifc/blob/main/src/cpp/web-ifc-wasm.cpp
// https://github.com/ThatOpen/engine_web-ifc/blob/main/src/cpp/web-ifc-test.cpp
// 
// It is build based on what the needs of Speckle are:
// https://github.com/specklesystems/speckle-server/blob/main/packages/fileimport-service/ifc/parser_v2.js#L26

/*
	const lines = await this.ifcapi.GetLineIDsWithType(this.modelId, element)
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
using namespace msclr::interop;
using namespace webifc::manager;
using namespace webifc::parsing;
using namespace webifc::geometry;

namespace  WebIfcClrWrapper
{
    public ref struct Buffer
    {
        IntPtr DataPtr;
        int Size;
        int ElementSize;

        Buffer(IntPtr dataPtr, int size, int elementSize) {
            DataPtr = dataPtr;
            Size = size;
            ElementSize = elementSize;
        }
    };

    public struct Vertex
    {
        double Vx;
        double Vy;
        double Vz;
        double Nx;
        double Ny;
        double Nz;
    };

    public ref class Mesh
    {
    private:

        IfcGeometry* geometry;
        uint32_t expressID;

        template<typename T>
        Buffer^ ToBuffer(std::vector<T>& vec) {
            return gcnew Buffer(
                IntPtr(vec.data()),
                (int)vec.size(),
                sizeof(vec[0]));
        }

    public:

        Mesh(IfcGeometry* geom, uint32_t expressID) {
            this->geometry = geom;
            this->expressID = expressID;
        }

        uint32_t GetExpressID() {
            return expressID;
        }

        Buffer^ GetVertexData() {
            return ToBuffer(geometry->vertexData);
        }

        Buffer^ GetIndexData() {
            return ToBuffer(geometry->indexData);
        }
    };

    public ref struct Color
    {
        double R;
        double G;
        double B;
        double A;

        Color(double r, double g, double b, double a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    };

    public ref class TransformedMesh
    {
    public:
        Mesh^ Mesh;
        Color^ Color;
        array<double>^ Transform;
    };

    public ref class MeshList
    {
    private:
        IfcFlatMesh* nativeFlatMesh;

    public:

        uint32_t ExpressID;

        MeshList(IfcFlatMesh* mesh, uint32_t expressID) {
            this->nativeFlatMesh = mesh;
            this->ExpressID = expressID;
            Meshes = gcnew List<TransformedMesh^>(mesh->geometries.size());
        }

        List<TransformedMesh^>^ Meshes;
    };

    public ref class Model
    {
    private:

        ModelManager* manager;
        IfcLoader* loader;
        IfcGeometryProcessor* geometryProcessor;        

    public:

        int Id;

        Model(ModelManager* mm, int id, IfcLoader* loader) {
            this->manager = mm;
            this->Id = id;
            this->geometryProcessor = manager->GetGeometryProcessor(id);
            this->loader = loader;
        }

        int Size() {
            return loader->GetTotalSize();
        }

        List<MeshList^>^ GetMeshes() {   
            auto r = gcnew List<MeshList^>(2);

            for (auto type : manager->GetSchemaManager().GetIfcElementList())
            {
                // TODO: maybe some of these elments are desired. 
                if (type == webifc::schema::IFCOPENINGELEMENT 
                    || type == webifc::schema::IFCSPACE 
                    || type == webifc::schema::IFCOPENINGSTANDARDCASE)
                {
                    continue;
                }
                
                auto typeName = marshal_as<System::String^>(manager->GetSchemaManager().IfcTypeCodeToType(type));
                
                // TEMP: debugging
                //System::Console::WriteLine("Processing types: " + typeName);

                for (auto e : loader->GetExpressIDsWithType(type))
                {
                    auto flatMesh = geometryProcessor->GetFlatMesh(e);
                    auto meshList = gcnew MeshList(&flatMesh, e);
                    for (auto& placedGeom : flatMesh.geometries)
                    {
                        auto mesh = Convert(placedGeom);
                        meshList->Meshes->Add(mesh);
                    }                  
                    r->Add(meshList);
                }
            }

            return r;
        }

        TransformedMesh^ Convert(IfcPlacedGeometry& pg) {
            auto r = gcnew TransformedMesh();
            r->Mesh = GetMesh(pg.geometryExpressID);
            r->Color = gcnew Color(pg.color.r, pg.color.g, pg.color.b, pg.color.a);
            r->Transform = gcnew array<double>(16);
            pg.SetFlatTransformation();
            for (int i = 0; i < 16; i++) {
                r->Transform[i] = pg.flatTransformation[i];
            }
            return r;
        }

        Mesh^ GetMesh(uint32_t expressID) {
            return gcnew Mesh(&geometryProcessor->GetGeometry(expressID), expressID);
        }

        uint32_t GetLineType(uint32_t expressID) {
            return loader->GetLineType(expressID);
        }

        uint32_t GetMaxExpressID() {
            return loader->GetMaxExpressId();
        }

        List<int>^ GetAllLines() {
            auto lines = loader->GetAllLines();
            auto list = gcnew List<int>(lines.size());
            for (auto line : lines)
                list->Add(line);
            return list;
        }
    };

    public ref class DotNetApi
    {
    public:
        const bool MT_ENABLED = false;

        webifc::manager::LoaderSettings* settings
            = new webifc::manager::LoaderSettings();

        webifc::manager::ModelManager* manager
            = new webifc::manager::ModelManager(MT_ENABLED);
      
        Model^ Load(System::String^ fileName) {
            auto modelID = manager->CreateModel(*settings);
            auto loader = manager->GetIfcLoader(modelID);
            std::ifstream ifs;
            std::string unmanaged = marshal_as<std::string>(fileName);
            ifs.open(unmanaged, std::ifstream::in);
            loader->LoadFile(ifs);
            return gcnew Model(manager, modelID, loader);
        }
       
        std::vector<IfcCrossSections> GetAllCrossSections(uint32_t modelID, uint8_t dimensions) {
            if (!manager->IsModelOpen(modelID)) return std::vector<IfcCrossSections>();
            auto geomLoader = manager->GetGeometryProcessor(modelID);

            std::vector<uint32_t> typeList;
            typeList.push_back(webifc::schema::IFCSECTIONEDSOLIDHORIZONTAL);
            typeList.push_back(webifc::schema::IFCSECTIONEDSOLID);
            typeList.push_back(webifc::schema::IFCSECTIONEDSURFACE);

            std::vector<IfcCrossSections> crossSections;

            for (auto& type : typeList)
            {
                auto elements = manager->GetIfcLoader(modelID)->GetExpressIDsWithType(type);

                for (size_t i = 0; i < elements.size(); i++)
                {
                    IfcCrossSections crossSection;
                    if (dimensions == 2) crossSection = geomLoader->GetLoader().GetCrossSections2D(elements[i]);
                    else crossSection = geomLoader->GetLoader().GetCrossSections3D(elements[i]);
                    crossSections.push_back(crossSection);
                }
            }

            return crossSections;
        }

        std::vector<IfcAlignment> GetAllAlignments(uint32_t modelID)
        {
            if (!manager->IsModelOpen(modelID)) return std::vector<IfcAlignment>();
            auto geomLoader = manager->GetGeometryProcessor(modelID);
            auto type = webifc::schema::IFCALIGNMENT;

            auto elements = manager->GetIfcLoader(modelID)->GetExpressIDsWithType(type);

            std::vector<IfcAlignment> alignments;

            for (size_t i = 0; i < elements.size(); i++)
            {
                IfcAlignment alignment = geomLoader->GetLoader().GetAlignment(elements[i]);
                alignment.transform(geomLoader->GetCoordinationMatrix());
                alignments.push_back(alignment);
            }

            return alignments;
        }

        std::array<double, 16> GetCoordinationMatrix(uint32_t modelID)
        {
            return  manager->IsModelOpen(modelID)
                ? manager->GetGeometryProcessor(modelID)->GetFlatCoordinationMatrix()
                : std::array<double, 16>();
        }
               
        System::String^ GetNameFromTypeCode(uint32_t type) {
            return marshal_as<System::String^>(manager->GetSchemaManager().IfcTypeCodeToType(type));
        }

        uint32_t GetTypeCodeFromName(std::string typeName) {
            return manager->GetSchemaManager().IfcTypeToTypeCode(typeName);
        }

        bool IsIfcElement(uint32_t type) {
            return manager->GetSchemaManager().IsIfcElement(type);
        }

        /*
        void ReadValue(uint32_t modelID, webifc::parsing::IfcTokenType t)
        {
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
        }
            */

        /*
    void GetArgs(uint32_t modelID)
        {
            bool inObject = false; 
            bool inList = false;
            
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
        }
        */

        /*
        void GetLine(uint32_t modelID, uint32_t expressID)
        {
            auto loader = manager->GetIfcLoader(modelID);
            uint32_t lineType = loader->GetLineType(expressID);
            loader->MoveToArgumentOffset(expressID, 0);

            //GetArgs(modelID);

            // TODO: convert to a struct

            auto retVal = emscripten::val::object();
            retVal.set(emscripten::val("ID"), expressID);
            retVal.set(emscripten::val("type"), lineType);
            retVal.set(emscripten::val("arguments"), arguments);
            return retVal;
        }
        */
          

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