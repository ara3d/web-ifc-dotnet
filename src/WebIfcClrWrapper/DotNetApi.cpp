/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. 
 */

/* 
* This file was originally authored by Christopher Diggins of Ara 3D Inc. 
* for Speckle Systems Ltd.
* 
* This is a C++/CLI wrapper around the Web-IFC component library 
* by Tom van Diggelen and That Open Company. 
* 
* It is built based on the specific needs of a Speckle IFC import service:
* https://github.com/specklesystems/speckle-server/blob/main/packages/fileimport-service/ifc/parser_v2.js#L26
* 
* And was inspired by:
* - https://github.com/ThatOpen/engine_web-ifc/blob/main/src/ts/web-ifc-api.ts
* - https://github.com/ThatOpen/engine_web-ifc/blob/main/src/cpp/web-ifc-wasm.cpp
* - https://github.com/ThatOpen/engine_web-ifc/blob/main/src/cpp/web-ifc-test.cpp

*/

#pragma unmanaged
#include <string>
#include <algorithm>
#include <vector>
#include <stack>
#include <cstdint>
#include <memory>
#include "../engine_web-ifc/src/cpp/modelmanager/ModelManager.h"
#include "../engine_web-ifc/src/cpp/version.h"
#include <iostream>
#include <fstream>

#pragma managed 

#include <msclr\marshal_cppstd.h>

using namespace System;
using namespace System::Collections::Generic;
using namespace msclr::interop;
using namespace webifc::manager;
using namespace webifc::parsing;
using namespace webifc::geometry;
using namespace webifc::schema;

namespace  WebIfcClrWrapper
{
    // Forward declarations of classes
    ref class Model;

    // Forward declaration of functions
    Model^ CreateModel(ModelManager* manager, int modelId, IfcLoader* loader);

    /// <summary>
    /// Wrapper for vertex or index data from a mesh. 
    /// Allows caller to not have to copy data.
    /// </summary>
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

    /// <summary>
    /// Parsed data from an IFC line.
    /// Arguments might be one of:
    /// - String
    /// - EnumValue
    /// - RefValue
    /// - List of Object^
    /// - Long
    /// - Double
    /// </summary>
    public ref class LineData
    {
    public:
        int32_t ExpressId = 0;
        String^ Type = nullptr;
        List<Object^>^ Arguments = gcnew List<Object^>(0);
    };

    /// <summary>
    /// Rerpresents an embedded labeled group in the input data 
    /// </summary>
    public ref class LabelValue
    {
    public:
        String^ Type;
        List<Object^>^ Arguments;
        LabelValue(String^ type, List<Object^>^ args)
            : Type(type), Arguments(args) { }
    };

    /// <summary>
    /// Wrapper around an enum value.
    /// usually found as an argument  
    /// </summary>
    public ref class EnumValue
    {
    public:
        String^ Name;
        EnumValue(String^ name) : Name(name) {}
    };

    /// <summary>
    /// The RefValue is a wrapper around an express Id.
    /// </summary>
    public ref class RefValue
    {
    public:
        uint32_t ExpressId;
        RefValue(uint32_t expressId) : ExpressId(expressId) {}
    };

    /// <summary>
    /// This is the layout of vertex data, as it is stored in the web-ifc engine.
    /// </summary>
    public struct Vertex
    {
        double Vx, Vy, Vz;
        double Nx, Ny, Nz;
    };

    /// <summary>
    /// This is a .NET wrapper around the Web-IFC engine. 
    /// </summary>
    public ref class DotNetApi
    {
    public:
        const bool MT_ENABLED = false;

        static IfcSchemaManager* schemaManager = new IfcSchemaManager();

        void DisposeAll()
        {
            delete settings;
            delete manager;
            delete schemaManager;
        }

        webifc::manager::LoaderSettings* settings
            = new webifc::manager::LoaderSettings();

        webifc::manager::ModelManager* manager
            = new webifc::manager::ModelManager(MT_ENABLED);

        Model^ Load(String^ fileName) {
            manager->SetLogLevel(6);
            auto modelId = manager->CreateModel(*settings);
            auto loader = manager->GetIfcLoader(modelId);
            std::ifstream ifs;
            std::string unmanaged = marshal_as<std::string>(fileName);
            ifs.open(unmanaged, std::ifstream::in);
            loader->LoadFile(ifs);
            return CreateModel(manager, modelId, loader);
        }

        static String^ GetNameFromTypeCode(uint32_t type) {
            return marshal_as<String^>(schemaManager->IfcTypeCodeToType(type));
        }

        static uint32_t GetTypeCodeFromName(std::string typeName) {
            return schemaManager->IfcTypeToTypeCode(typeName);
        }

        static bool IsIfcElement(uint32_t type) {
            return schemaManager->IsIfcElement(type);
        }

        static List<Object^>^ GetArgs(IfcLoader* loader) {
            return GetArgs(loader, false);
        }

        static List<Object^>^ GetArgs(IfcLoader* loader, bool inObject) {
            return GetArgs(loader, inObject, false);
        }

        static List<Object^>^ GetArgs(IfcLoader* loader, bool inObject, bool inList) {
            auto arguments = gcnew List<Object^>(0);
            bool endOfLine = false;

            while (!loader->IsAtEnd() && !endOfLine)
            {
                try
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
                        arguments->Add(nullptr);
                        break;
                    }
                    case webifc::parsing::IfcTokenType::SET_BEGIN:
                    {
                        arguments->Add(GetArgs(loader, false, true));
                        break;
                    }
                    case webifc::parsing::IfcTokenType::SET_END:
                    {
                        endOfLine = true;
                        break;
                    }
                    case webifc::parsing::IfcTokenType::LABEL:
                    {
                        loader->StepBack();
                        auto s = marshal_as<String^>(std::string(loader->GetStringArgument()));
                        loader->GetTokenType();
                        arguments->Add(gcnew LabelValue(s, GetArgs(loader, true)));
                        break;
                    }
                    case webifc::parsing::IfcTokenType::STRING:
                    {
                        loader->StepBack();
                        arguments->Add(marshal_as<String^>(loader->GetDecodedStringArgument()));
                        break;
                    }
                    case webifc::parsing::IfcTokenType::ENUM:
                    {
                        loader->StepBack();
                        arguments->Add(gcnew EnumValue(marshal_as<String^>(std::string(loader->GetStringArgument()))));
                        break;
                    }
                    case webifc::parsing::IfcTokenType::REAL:
                    {
                        loader->StepBack();
                        arguments->Add(loader->GetDoubleArgument());
                        break;
                    }
                    case webifc::parsing::IfcTokenType::INTEGER:
                    {
                        loader->StepBack();
                        // TEMP: this might be a "1."? 
                        arguments->Add(loader->GetIntArgument());
                        break;
                    }
                    case webifc::parsing::IfcTokenType::REF:
                    {
                        loader->StepBack();
                        arguments->Add(gcnew RefValue(loader->GetRefArgument()));
                        break;
                    }
                    default:
                    {
                        //??
                    }
                    }
                }
                catch (const std::exception& e)
                {
                    System::Diagnostics::Debug::WriteLine(gcnew String(e.what()));
                }
            }
            return arguments;
        };
    };

    /// <summary>
    /// Provides access to parsed and tessellated geometry data from the web-ifc engine. 
    /// </summary>
    public ref class Mesh
    {
    private:

        IfcGeometry* geometry;
        uint32_t expressId;

        template<typename T>
        Buffer^ ToBuffer(std::vector<T>& vec) {
            return gcnew Buffer(
                IntPtr(vec.data()),
                (int)vec.size(),
                sizeof(vec[0]));
        }

    public:

        Mesh(IfcGeometry* geom, uint32_t expressId) {
            this->geometry = geom;
            this->expressId = expressId;
        }

        uint32_t GetExpressId() {
            return expressId;
        }

        Buffer^ GetVertexData() {
            return ToBuffer(geometry->vertexData);
        }

        Buffer^ GetIndexData() {
            return ToBuffer(geometry->indexData);
        }
    };

    /// <summary>
    /// Color data. 
    /// </summary>
    public ref struct Color
    {
        double R, G, B, A;

        Color(double r, double g, double b, double a)
            : R(r), G(g), B(b), A(a) { }
    };

    /// <summary>
    /// A mesh with color and a global transformation matrix.
    /// It hasn't yet been determined whether the mesh is colum-row, or row-column
    /// </summary>
    public ref class TransformedMesh
    {
    public:
        Mesh^ Mesh;
        Color^ Color;
        array<double>^ Transform;
    };

    /// <summary>
    /// This is a wrapper around the class in the underlying engine called an "IfcFlatMesh".
    /// It is just a list of of Transformed Meshes with an associated express Id. 
    /// It is perhaps called "flat" because it was originally a tree of references which 
    /// have been flattened into a list.
    /// </summary>
    public ref class MeshList
    {
    private:
        IfcFlatMesh* nativeFlatMesh;

    public:

        uint32_t ExpressId;

        MeshList(IfcFlatMesh* mesh, uint32_t expressId) {
            this->nativeFlatMesh = mesh;
            this->ExpressId = expressId;
            Meshes = gcnew List<TransformedMesh^>(mesh->geometries.size());
        }

        List<TransformedMesh^>^ Meshes;
    };

    /// <summary>
    /// A model is an abstraction of the web-ifc engine concept of Model ID
    /// with convenience methods. This makes programming against the system 
    /// easier. 
    /// </summary>
    public ref class Model
    {
    private:

        ModelManager* manager;
        IfcLoader* loader;
        IfcGeometryProcessor* geometryProcessor;        

    public:

        int Id;

        Model(ModelManager* mm, int Id, IfcLoader* loader) {
            this->manager = mm;
            this->Id = Id;
            this->geometryProcessor = manager->GetGeometryProcessor(Id);
            this->loader = loader;
        }

        int Size() {
            return loader->GetTotalSize();
        }

        List<MeshList^>^ GetMeshes() {   
            auto r = gcnew List<MeshList^>(2);

            for (auto type : DotNetApi::schemaManager->GetIfcElementList())
            {
                // TODO: maybe some of these elments are desired. 
                if (type == IFCOPENINGELEMENT 
                    || type == IFCSPACE 
                    || type == IFCOPENINGSTANDARDCASE)
                {
                    continue;
                }
                
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

        Mesh^ GetMesh(uint32_t expressId) {
            return gcnew Mesh(&geometryProcessor->GetGeometry(expressId), expressId);
        }

        uint32_t GetLineType(uint32_t expressId) {
            return loader->GetLineType(expressId);
        }

        uint32_t GetMaxExpressId() {
            return loader->GetMaxExpressId();
        }

        List<uint32_t>^ GetLineIds() {
            auto lines = loader->GetAllLines();
            auto list = gcnew List<uint32_t>(lines.size());
            for (auto line : lines)
                list->Add(line);
            return list;
        }

        LineData^ GetLineData(uint32_t expressId) {
            loader->MoveToArgumentOffset(expressId, 0);
            auto lineType = GetLineType(expressId);            
           	auto lineData = gcnew LineData();
            lineData->ExpressId = expressId;
            lineData->Type = DotNetApi::GetNameFromTypeCode(lineType);
            lineData->Arguments = DotNetApi::GetArgs(loader);
            return lineData;
        }
    };      

    Model^ CreateModel(ModelManager* manager, int modelId, IfcLoader* loader)
    {
        return gcnew Model(manager, modelId, loader);
    }
}