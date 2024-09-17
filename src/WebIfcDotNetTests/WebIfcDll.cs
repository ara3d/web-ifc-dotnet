using System.Runtime.InteropServices;

namespace WebIfcDotNetTests
{

    public static class WebIfcDll
    {
        private const string DllName = "web-ifc-dll.dll"; // Replace with the name of your DLL

        // InitializeApi
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr InitializeApi();

        // FinalizeApi
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FinalizeApi(IntPtr api);

        // LoadModel
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr LoadModel(IntPtr api, string fileName);

        // GetGeometry
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetGeometry(IntPtr api, IntPtr model, uint id);

        // GetNumMeshes
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetNumMeshes(IntPtr api, IntPtr geometry);

        // GetMesh
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetMesh(IntPtr api, IntPtr geometry, int index);

        // GetTransform
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetTransform(IntPtr api, IntPtr mesh);

        // GetColor
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetColor(IntPtr api, IntPtr mesh);

        // GetNumVertices
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetNumVertices(IntPtr api, IntPtr mesh);

        // GetVertices
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetVertices(IntPtr api, IntPtr mesh);

        // GetNumIndices
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetNumIndices(IntPtr api, IntPtr mesh);

        // GetIndices
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetIndices(IntPtr api, IntPtr mesh);
    }
}