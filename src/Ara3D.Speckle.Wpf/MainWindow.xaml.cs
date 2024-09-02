// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ara3D.Logging;
using Ara3D.Speckle.Data;
using HelixToolkit.Wpf;
using NUnit.Framework.Constraints;
using Objects.Geometry;
using Objects.Other;
using Objects.Utils;
using Serilog;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using WebIfcClrWrapper;
using WebIfcDotNet;
using WebIfcDotNetTests;
using Color = System.Windows.Media.Color;
using Mesh = Objects.Geometry.Mesh;
using SpeckleObject = Ara3D.Speckle.Data.SpeckleObject;

namespace Ara3D.Speckle.Wpf
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        private Point3D _currentPosition;

        public Point3D CurrentPosition
        {
            get
            {
                return this._currentPosition;
            }
            set
            {
                this._currentPosition = value;
                RaisePropertyChanged("CurrentPosition");
            }
        }

        private void CreateBoxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var diceMesh = new MeshBuilder();
            diceMesh.AddBox(new Point3D(0, 0, 0), 1, 1, 1);
            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            for (int k = 0; k < 2; k++)
            {
                var points = new List<Point3D>();
                diceMesh.ChamferCorner(new Point3D(i - 0.5, j - 0.5, k - 0.5), 0.1, 1e-6, points);
                //foreach (var p in points)
                //    b.ChamferCorner(p, 0.03);
            }

            var model3d = new GeometryModel3D { Geometry = diceMesh.ToMesh(), Material = Materials.Green };
            var model = new MeshGeometryVisual3D() { Content =  model3d };
            this.Viewport.Children.Add(model);
        }

        private async void OpenRemoteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1);

            // The id of the stream to work with (we're assuming it already exists in your default account's server)
            //var streamId = "51d8c73c9d";
            //var streamId = "97529188be"; 

            // Advanced Revit Project 
            //var streamId = "8f64180899";

            // Default Speckl architecture 
            var streamId = "3247bdd4ee";

            // The name of the branch we'll receive data from.
            var branchName = "base design";

            // Get default account on this machine
            // If you don't have Speckle Manager installed download it from https://speckle-releases.netlify.app
            var defaultAccount = AccountManager.GetDefaultAccount();

            // Or get all the accounts and manually choose the one you want
            // var accounts = AccountManager.GetAccounts();
            // var defaultAccount = accounts.ToList().FirstOrDefault();

            if (defaultAccount == null)
                throw new Exception("Could not find a default account. You may need to install and run the Speckle Manager");

            // Authenticate using the account
            using var client = new Client(defaultAccount);

            // Get the main branch with it's latest commit reference
            var branch = await client.BranchGet(streamId, branchName, 1);

            // Get the id of the object referenced in the commit
            var hash = branch.commits.items[0].referencedObject;

            // Create the server transport for the specified stream.
            var transport = new ServerTransport(defaultAccount, streamId);

            // Receive the object
            var root = await Operations.Receive(hash, transport);

            await ConvertToMeshes(root);
        }

        public Color GetRenderMaterialColor(RenderMaterial material)
        {
            if (material == null)
                return Colors.DarkSlateGray;
            return Color.FromArgb((byte)(material.opacity * 255), material.diffuseColor.R, material.diffuseColor.G,
                material.diffuseColor.B);
        }
        
        public async Task AddMeshes(SpeckleObject speckle)
        {
            await Application.Current.Dispatcher.BeginInvoke(async () =>
            {
                var parentVisual = new SortingVisual3D();
                Viewport.Children.Add(parentVisual);
                await CreateModels(parentVisual.Children, speckle);
            });
        }

        public async Task CreateModels(Visual3DCollection parent, SpeckleObject speckle)
        {
            throw new NotImplementedException();
            /*
            if (speckle.Base is Mesh m)
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    var meshGeo = ToMeshGeometry3D(m);
                    var rm = m["renderMaterial"] as RenderMaterial;
                    var color = GetRenderMaterialColor(rm);
                    var mat = new DiffuseMaterial(new SolidColorBrush(color));
                    var model = new GeometryModel3D(meshGeo, mat);
                    var tmp = new ModelVisual3D() { Content = model };
                    parent.Add(tmp);
                    parent = tmp.Children;
                });
            }

            foreach (var child in speckle.Elements)
                await CreateModels(parent, child);
            */
        }

        public Point3D GetPoint(Mesh mesh, int i)
        {
            var ix = i * 3 + 0;
            var iy = i * 3 + 1;
            var iz = i * 3 + 2;
            var scale = 0.01;
            return new Point3D(
                mesh.vertices[ix] * scale, 
                mesh.vertices[iy] * scale, 
                mesh.vertices[iz] * scale);
        }

        public MeshGeometry3D ToMeshGeometry3D(Mesh mesh)
        {
            mesh.TriangulateMesh();

            var mb = new MeshBuilder();
            for (var i = 0; i < mesh.faces.Count; i += 4)
            {
                var faceSize = mesh.faces[i];
                if (faceSize != 3) throw new Exception("Forgot to triangulate the mesh");
                var v0 = GetPoint(mesh, mesh.faces[i + 1]);
                var v1 = GetPoint(mesh, mesh.faces[i + 2]);
                var v2 = GetPoint(mesh, mesh.faces[i + 3]);
                mb.AddTriangle(v0, v1, v2);
            }

            var r = mb.ToMesh();
            r.CalculateNormals();
            return r;
        }

        private async void OpenLocalMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1);

            var filePath = @"C:\Users\cdigg\AppData\Local\Temp\Speckle";

            // Offload the long-running operation to a separate thread
            await Task.Run(async () =>
            {
                var localSql = new SQLiteTransport(filePath);
                var root = await Operations.Receive("f0fa094f0c24fba78171bd57816f3797", localSql);

                // Update the UI with the result
                await ConvertToMeshes(root);
            });
        }

        public async Task ConvertToMeshes(Base root)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            SpeckleObject so = null;
            var converter = await Task.Run(() => so = root.ToSpeckleObject());

            // Process the object however you'd like
            await AddMeshes(so);
        }

        private async void OpenIfcMenuItem_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
            /*
            var api = new DotNetApi();
            var logger = new Logger(LogWriter.ConsoleWriter, "");
            var f = "C:\\Users\\cdigg\\git\\web-ifc-dotnet\\src\\engine_web-ifc\\tests\\ifcfiles\\public\\AC20-FZK-Haus.ifc";
            var g = ModelGraph.Load(api, logger, f);
            var b = g.ToSpeckle();
            var c = await SpeckleConverter.Create(b);
            await AddMeshes(c.Root);*/
        }
    }
}