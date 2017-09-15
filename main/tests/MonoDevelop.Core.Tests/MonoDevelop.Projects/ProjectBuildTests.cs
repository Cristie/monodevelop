﻿//
// ProjectBuildTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.IO;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ProjectBuildTests: TestBase
	{
		[Test]
		public async Task BuildConsoleProject()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution("console-project-msbuild");
			await sol.SaveAsync(Util.GetMonitor());

			// Ensure the project is buildable
			var result = await sol.Build(Util.GetMonitor(), "Debug");
			Assert.AreEqual(0, result.ErrorCount, "#1");

			sol.Dispose();
		}

		[Test ()]
		public async Task BuildingAndCleaningSolution ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject p = (DotNetProject)sol.FindProjectByName ("console-with-libs");
			DotNetProject lib1 = (DotNetProject)sol.FindProjectByName ("library1");
			DotNetProject lib2 = (DotNetProject)sol.FindProjectByName ("library2");

			SolutionFolder folder = new SolutionFolder ();
			folder.Name = "subfolder";
			sol.RootFolder.Items.Add (folder);
			sol.RootFolder.Items.Remove (lib2);
			folder.Items.Add (lib2);

			Workspace ws = new Workspace ();
			ws.FileName = Path.Combine (sol.BaseDirectory, "workspace");
			ws.Items.Add (sol);
			await ws.SaveAsync (Util.GetMonitor ());

			// Build the project and the references

			BuildResult res = await ws.Build (Util.GetMonitor (), ConfigurationSelector.Default);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);

			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));

			// Clean the workspace

			await ws.Clean (Util.GetMonitor (), ConfigurationSelector.Default);
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));

			// Build the solution

			res = await ws.Build (Util.GetMonitor (), ConfigurationSelector.Default);
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (3, res.BuildCount);

			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsTrue (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));

			// Clean the solution

			await sol.Clean (Util.GetMonitor (), "Debug");
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));

			// Build the solution folder

			res = await folder.Build (Util.GetMonitor (), (SolutionConfigurationSelector)"Debug");
			Assert.AreEqual (0, res.ErrorCount);
			Assert.AreEqual (0, res.WarningCount);
			Assert.AreEqual (1, res.BuildCount);

			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", "console-with-libs.exe")));
			Assert.IsFalse (File.Exists (Util.Combine (p.BaseDirectory, "bin", "Debug", GetMdb (p, "console-with-libs.exe"))));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", "library1.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib1.BaseDirectory, "bin", "Debug", GetMdb (lib1, "library1.dll"))));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsTrue (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));

			// Clean the solution folder

			await folder.Clean (Util.GetMonitor (), (SolutionConfigurationSelector)"Debug");
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", "library2.dll")));
			Assert.IsFalse (File.Exists (Util.Combine (lib2.BaseDirectory, "bin", "Debug", GetMdb (lib2, "library2.dll"))));
			sol.Dispose ();
		}

		[Test ()]
		public async Task BuildConfigurationMappings ()
		{
			string solFile = Util.GetSampleProject ("test-build-configs", "test-build-configs.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			DotNetProject lib1 = (DotNetProject)sol.FindProjectByName ("Lib1");
			DotNetProject lib2 = (DotNetProject)sol.FindProjectByName ("Lib2");
			DotNetProject lib3 = (DotNetProject)sol.FindProjectByName ("Lib3");
			DotNetProject lib4 = (DotNetProject)sol.FindProjectByName ("Lib4");

			// Check that the output file names are correct

			Assert.AreEqual (GetConfigFolderName (lib1, "Debug"), "Debug");
			Assert.AreEqual (GetConfigFolderName (lib1, "Release"), "Release");
			Assert.AreEqual (GetConfigFolderName (lib2, "Debug"), "Release");
			Assert.AreEqual (GetConfigFolderName (lib2, "Release"), "Debug");
			Assert.AreEqual (GetConfigFolderName (lib3, "Debug"), "DebugExtra");
			Assert.AreEqual (GetConfigFolderName (lib3, "Release"), "ReleaseExtra");

			// Check that building the solution builds the correct project configurations

			await CheckSolutionBuildClean (sol, "Debug");
			await CheckSolutionBuildClean (sol, "Release");

			// Check that building a project builds the correct referenced project configurations

			await CheckProjectReferencesBuildClean (sol, "Debug");
			await CheckProjectReferencesBuildClean (sol, "Release");

			// Single project build and clean

			await CheckProjectBuildClean (lib2, "Debug");
			await CheckProjectBuildClean (lib2, "Release");
			await CheckProjectBuildClean (lib3, "Debug");
			await CheckProjectBuildClean (lib3, "Release");
			await CheckProjectBuildClean (lib4, "Debug");
			await CheckProjectBuildClean (lib4, "Release");

			sol.Dispose ();
		}

		async Task CheckSolutionBuildClean (Solution sol, string configuration)
		{
			SolutionConfigurationSelector config = (SolutionConfigurationSelector)configuration;
			string tag = "CheckSolutionBuildClean config=" + configuration;
			DotNetProject lib1 = (DotNetProject)sol.FindProjectByName ("Lib1");
			DotNetProject lib2 = (DotNetProject)sol.FindProjectByName ("Lib2");
			DotNetProject lib3 = (DotNetProject)sol.FindProjectByName ("Lib3");
			DotNetProject lib4 = (DotNetProject)sol.FindProjectByName ("Lib4");

			Assert.IsFalse (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib4.GetOutputFileName (config)), tag);

			BuildResult res = await sol.Build (Util.GetMonitor (), config);
			Assert.AreEqual (0, res.WarningCount, tag);
			Assert.AreEqual (0, res.ErrorCount, tag);

			Assert.IsTrue (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib4.GetOutputFileName (config)), tag);

			await sol.Clean (Util.GetMonitor (), config);

			Assert.IsFalse (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib4.GetOutputFileName (config)), tag);
		}

		async Task CheckProjectReferencesBuildClean (Solution sol, string configuration)
		{
			SolutionConfigurationSelector config = (SolutionConfigurationSelector)configuration;
			string tag = "CheckProjectReferencesBuildClean config=" + configuration;
			DotNetProject lib1 = (DotNetProject)sol.FindProjectByName ("Lib1");
			DotNetProject lib2 = (DotNetProject)sol.FindProjectByName ("Lib2");
			DotNetProject lib3 = (DotNetProject)sol.FindProjectByName ("Lib3");
			DotNetProject lib4 = (DotNetProject)sol.FindProjectByName ("Lib4");

			Assert.IsFalse (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsFalse (File.Exists (lib4.GetOutputFileName (config)), tag);

			BuildResult res = await lib1.Build (Util.GetMonitor (), config, true);
			Assert.AreEqual (0, res.WarningCount, tag);
			Assert.AreEqual (0, res.ErrorCount, tag + " " + res.CompilerOutput);

			Assert.IsTrue (File.Exists (lib1.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib2.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib3.GetOutputFileName (config)), tag);
			Assert.IsTrue (File.Exists (lib4.GetOutputFileName (config)), tag);

			await sol.Clean (Util.GetMonitor (), config);
		}

		async Task CheckProjectBuildClean (DotNetProject lib, string configuration)
		{
			SolutionConfigurationSelector config = (SolutionConfigurationSelector)configuration;
			string tag = "CheckProjectBuildClean lib=" + lib.Name + " config=" + configuration;

			Assert.IsFalse (File.Exists (lib.GetOutputFileName (config)), tag);

			BuildResult res = await lib.Build (Util.GetMonitor (), config, false);
			Assert.AreEqual (0, res.WarningCount, tag);
			Assert.AreEqual (0, res.ErrorCount, tag);

			Assert.IsTrue (File.Exists (lib.GetOutputFileName (config)), tag);

			await lib.Clean (Util.GetMonitor (), config);
			Assert.IsFalse (File.Exists (lib.GetOutputFileName (config)), tag);
		}

		public static string GetMdb (Project p, string file)
		{
			return ((DotNetProject)p).GetAssemblyDebugInfoFile (p.Configurations [0].Selector, file);
		}

		string GetConfigFolderName (DotNetProject lib, string conf)
		{
			return Path.GetFileName (Path.GetDirectoryName (lib.GetOutputFileName ((SolutionConfigurationSelector)conf)));
		}

		[Test]
		public async Task BuildSolutionWithUnsupportedProjects ()
		{
			string solFile = Util.GetSampleProject ("unsupported-project", "console-with-libs.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var res = await sol.Build (Util.GetMonitor (), "Debug");

			// The solution has a console app that references an unsupported library. The build of the solution should fail.
			Assert.AreEqual (1, res.ErrorCount);

			var app = sol.GetAllItems<DotNetProject> ().FirstOrDefault (it => it.FileName.FileName == "console-with-libs.csproj");

			// The console app references an unsupported library. The build of the project should fail.
			res = await app.Build (Util.GetMonitor (), ConfigurationSelector.Default, true);
			Assert.IsTrue (res.ErrorCount == 1);

			// A solution build should succeed if it has unbuildable projects but those projects are not referenced by buildable projects
			app.References.Clear ();
			await sol.SaveAsync (Util.GetMonitor ());
			res = await sol.Build (Util.GetMonitor (), "Debug");
			Assert.IsTrue (res.ErrorCount == 0);

			// Regular project not referencing anything else. Should build.
			res = await app.Build (Util.GetMonitor (), ConfigurationSelector.Default, true);
			Assert.IsTrue (res.ErrorCount == 0);

			sol.Dispose ();
		}

		[Test]
		public async Task BuildConsoleProjectAfterRename()
		{
			Solution sol = TestProjectsChecks.CreateConsoleSolution("console-project-msbuild");
			await sol.SaveAsync(Util.GetMonitor());

			// Ensure the project is still buildable with xbuild after a rename
			var project = sol.GetAllProjects().First();
			FilePath newFile = project.FileName.ParentDirectory.Combine("Test" + project.FileName.Extension);
			FileService.RenameFile(project.FileName, newFile.FileName);
			project.Name = "Test";

			var result = await sol.Build(Util.GetMonitor(), "Release");
			Assert.AreEqual(0, result.ErrorCount, "#2");

			sol.Dispose();
		}

		[Test]
		public async Task BuildWithCustomProps ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "project-with-custom-build-target.csproj");
			var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var ctx = new ProjectOperationContext ();
			ctx.GlobalProperties.SetValue ("TestProp", "foo");
			var res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, ctx);

			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed: foo", res.Errors [0].ErrorText);

			await p.Clean (Util.GetMonitor (), p.Configurations [0].Selector);
			res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, true);

			// Check that the global property is reset
			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed: show", res.Errors [0].ErrorText);

			p.Dispose ();
		}

		/// <summary>
		/// As above but the property is used to import different .targets files
		/// and MSBuild is used
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		public async Task BuildWithCustomProps2 ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "project-with-custom-build-target2.csproj");
			var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.RequiresMicrosoftBuild = true;

			var ctx = new ProjectOperationContext ();
			ctx.GlobalProperties.SetValue ("TestProp", "foo");
			var res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, ctx);

			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed (foo.targets): foo", res.Errors [0].ErrorText);

			await p.Clean (Util.GetMonitor (), p.Configurations [0].Selector);
			res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, true);

			// Check that the global property is reset
			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed (show.targets): show", res.Errors [0].ErrorText);

			p.Dispose ();
		}

		/// <summary>
		/// As above but the property has the same as a default global property defined
		/// by the IDE. This test makes sures the existing global properties are
		/// restored.
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		public async Task BuildWithCustomProps3 ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "project-with-custom-build-target3.csproj");
			var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);
			p.RequiresMicrosoftBuild = true;

			var ctx = new ProjectOperationContext ();
			ctx.GlobalProperties.SetValue ("BuildingInsideVisualStudio", "false");
			var res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, ctx);

			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed (false.targets): false", res.Errors [0].ErrorText);

			await p.Clean (Util.GetMonitor (), p.Configurations [0].Selector);
			res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector, true);

			// Check that the global property is reset
			Assert.AreEqual (1, res.Errors.Count);
			Assert.AreEqual ("Something failed (true.targets): true", res.Errors [0].ErrorText);

			p.Dispose ();
		}

		[Test]
		public async Task GetReferencedAssemblies ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "aliased-references.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var asms = (await p.GetReferencedAssemblies (p.Configurations [0].Selector)).ToArray ();

			var ar = asms.FirstOrDefault (a => a.FilePath.FileName == "System.Xml.dll");
			Assert.IsNotNull (ar);
			Assert.AreEqual ("", ar.Aliases);

			ar = asms.FirstOrDefault (a => a.FilePath.FileName == "System.Data.dll");
			Assert.IsNotNull (ar);
			Assert.AreEqual ("Foo", ar.Aliases);

			Assert.AreEqual (4, asms.Length);

			p.Dispose ();
		}

		[Test]
		public async Task GetReferences ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "aliased-references.csproj");
			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var asms = (await p.GetReferences (p.Configurations [0].Selector)).ToArray ();

			var ar = asms.FirstOrDefault (a => a.FilePath.FileName == "System.Xml.dll");
			Assert.IsNotNull (ar);
			Assert.AreEqual ("", ar.Aliases);

			ar = asms.FirstOrDefault (a => a.FilePath.FileName == "System.Data.dll");
			Assert.IsNotNull (ar);
			Assert.AreEqual ("Foo", ar.Aliases);

			Assert.AreEqual (4, asms.Length);

			p.Dispose ();
		}

		[Test]
		public async Task ProjectBuildAfterModifyingImport ()
		{
			// Tests that changes in imported target files are taken into account
			// when building a project.

			string solFile = Util.GetSampleProject ("test-project-refresh", "test-project-refresh.sln");
			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];

			var targetsFile = p.ItemDirectory.Combine ("extra.targets");
			var appFile = p.ItemDirectory.Combine ("Program.cs");

			// The project has a build error due to an invalid file inclusion in extra.targets

			var res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector);
			Assert.AreEqual (1, res.ErrorCount);

			// Fix the error in the targets file

			var txt = File.ReadAllText (targetsFile);
			txt = txt.Replace ("Extra.xx", "Extra.cs");
			File.WriteAllText (targetsFile, txt);

			// The project should now build. The builder should detect that the imported
			// extra.targets file has been modified and should reload it

			res = await p.Build (Util.GetMonitor (), p.Configurations [0].Selector);
			Assert.AreEqual (0, res.ErrorCount);
		}

		[Test ()]
		public async Task ProjectReferencingDisabledProject_SolutionBuildWorks ()
		{
			// If a project references another project that is disabled for the solution configuration it should
			// not be built when building the solution as a whole.

			// Build the solution. It should work.
			string solFile = Util.GetSampleProject ("invalid-reference-resolution", "InvalidReferenceResolution.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var res = await sol.Build (Util.GetMonitor (), "Debug");
			Assert.AreEqual (0, res.ErrorCount);

			sol.Dispose ();
		}

		[Test ()]
		public async Task ProjectReferencingConditionalReferences ()
		{
			string solFile = Util.GetSampleProject ("conditional-project-reference", "conditional-project-reference.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = sol.Items.FirstOrDefault (pr => pr.Name == "conditional-project-reference");

			Assert.AreEqual (2, p.GetReferencedItems ((SolutionConfigurationSelector)"DebugWin").Count ());
			Assert.AreEqual (1, p.GetReferencedItems ((SolutionConfigurationSelector)"Debug").Count ());

			//We have intentional compile error in windowsLib project
			var res = await p.Build (Util.GetMonitor (), (SolutionConfigurationSelector)"DebugWin", true);
			Assert.AreEqual (1, res.ErrorCount);

			res = await p.Build (Util.GetMonitor (), (SolutionConfigurationSelector)"Debug", true);
			Assert.AreEqual (0, res.ErrorCount);

			sol.Dispose ();
		}

		[Test ()]
		public async Task ProjectReferencingDisabledProject_ProjectBuildFails ()
		{
			// If a project references another project that is disabled for the solution configuration, the referenced
			// project should build when directly building the referencing project.

			// Build the solution. It should work.
			string solFile = Util.GetSampleProject ("invalid-reference-resolution", "InvalidReferenceResolution.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = sol.Items.FirstOrDefault (pr => pr.Name == "ReferencingProject");

			var res = await p.Build (Util.GetMonitor (), (SolutionConfigurationSelector)"Debug", true);
			Assert.AreEqual (1, res.ErrorCount);

			sol.Dispose ();
		}

		[Test]
		public async Task FastBuildCheckWithLibrary ()
		{
			string solFile = Util.GetSampleProject ("fast-build-test", "FastBuildTest.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var app = (DotNetProject)sol.Items [0];
			var lib = (DotNetProject)sol.Items [1];

			var cs = new SolutionConfigurationSelector ("Debug");

			Assert.IsTrue (app.FastCheckNeedsBuild (cs));
			Assert.IsTrue (lib.FastCheckNeedsBuild (cs));

			var res = await sol.Build (Util.GetMonitor (), cs);
			Assert.IsFalse (res.HasErrors);
			Assert.IsFalse (app.FastCheckNeedsBuild (cs));
			Assert.IsFalse (lib.FastCheckNeedsBuild (cs));

			var myClass = sol.ItemDirectory.Combine ("MyClass.cs");
			File.WriteAllText (myClass, "public class MyClass { public const string Message = \"Bye\" ; }");
			FileService.NotifyFileChanged (myClass);

			Assert.IsTrue (app.FastCheckNeedsBuild (cs));
			Assert.IsTrue (lib.FastCheckNeedsBuild (cs));

			res = await lib.Build (Util.GetMonitor (), cs);
			Assert.IsFalse (res.HasErrors);
			Assert.IsFalse (lib.FastCheckNeedsBuild (cs));
			Assert.IsTrue (app.FastCheckNeedsBuild (cs));

			res = await app.Build (Util.GetMonitor (), cs);
			Assert.IsFalse (res.HasErrors);
			Assert.IsFalse (lib.FastCheckNeedsBuild (cs));
			Assert.IsFalse (app.FastCheckNeedsBuild (cs));

			sol.Dispose ();
		}

		[Test]
		public async Task FastCheckNeedsBuildWithContext ()
		{
			string solFile = Util.GetSampleProject ("fast-build-test", "FastBuildTest.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var app = (DotNetProject)sol.Items [0];

			var cs = new SolutionConfigurationSelector ("Debug");

			var ctx = new TargetEvaluationContext ();
			ctx.GlobalProperties.SetValue ("Foo", "Bar");

			Assert.IsTrue (app.FastCheckNeedsBuild (cs, ctx));

			ctx = new TargetEvaluationContext ();
			ctx.GlobalProperties.SetValue ("Foo", "Bar");

			var res = await sol.Build (Util.GetMonitor (), cs, ctx);
			Assert.IsFalse (res.HasErrors);

			ctx = new TargetEvaluationContext ();
			ctx.GlobalProperties.SetValue ("Foo", "Bar");
			Assert.IsFalse (app.FastCheckNeedsBuild (cs, ctx));

			ctx = new TargetEvaluationContext ();
			ctx.GlobalProperties.SetValue ("Foo", "Modified");
			Assert.IsTrue (app.FastCheckNeedsBuild (cs, ctx));

			sol.Dispose ();
		}

		[Test]
		public async Task OnConfigureTargetEvaluationContext ()
		{
			var node = new CustomItemNode<EvalContextCreationTestExtension> ();
			WorkspaceObject.RegisterCustomExtension (node);
			EvalContextCreationTestExtension.ControlValue = "First";

			try {
				string solFile = Util.GetSampleProject ("fast-build-test", "FastBuildTest.sln");
				Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var app = (DotNetProject)sol.Items [0];

				var cs = new SolutionConfigurationSelector ("Debug");

				Assert.IsTrue (app.FastCheckNeedsBuild (cs));

				var res = await sol.Build (Util.GetMonitor (), cs);
				Assert.IsFalse (res.HasErrors);
				Assert.IsFalse (app.FastCheckNeedsBuild (cs));

				EvalContextCreationTestExtension.ControlValue = "Changed";
				Assert.IsTrue (app.FastCheckNeedsBuild (cs));

				res = await sol.Build (Util.GetMonitor (), cs);
				Assert.IsFalse (res.HasErrors);
				Assert.IsFalse (app.FastCheckNeedsBuild (cs));

				sol.Dispose ();
			} finally {
				WorkspaceObject.UnregisterCustomExtension (node);
			}
		}
	}

	[TestFixture]
	public class ProjectBuildTests_XBuild : ProjectBuildTests
	{
		[TestFixtureSetUp]
		public void SetUp ()
		{
			Runtime.Preferences.BuildWithMSBuild.Set (false);
		}

		[TestFixtureTearDown]
		public void Teardown ()
		{
			Runtime.Preferences.BuildWithMSBuild.Set (true);
		}
	}

	class EvalContextCreationTestExtension : ProjectExtension
	{
		public static string ControlValue = "First";

		internal protected override TargetEvaluationContext OnConfigureTargetEvaluationContext (string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			var c = base.OnConfigureTargetEvaluationContext (target, configuration, context);
			context.GlobalProperties.SetValue ("Foo", ControlValue);
			return c;
		}
	}
}
