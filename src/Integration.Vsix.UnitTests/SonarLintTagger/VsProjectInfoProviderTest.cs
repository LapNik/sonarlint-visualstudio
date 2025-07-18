﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class VsProjectInfoProviderTest
{
    private IServiceProvider serviceProvider;
    private TestLogger logger;
    private NoOpThreadHandler threadHandling;
    private const string FilePath = "file/path";
    private const string ProjectName = "projeccct";
    private static readonly Guid ProjectGuid = Guid.NewGuid();

    [TestInitialize]
    public void TestInitialize()
    {
        serviceProvider = Substitute.For<IServiceProvider>();
        logger = Substitute.ForPartsOf<TestLogger>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<VsProjectInfoProvider, IVsProjectInfoProvider>(
            MefTestHelpers.CreateExport<SVsServiceProvider>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<VsProjectInfoProvider>();

    [TestMethod]
    public void Ctor_DoesNotCallGetService()
    {
        var dte = SetUpCorrectDte(ProjectName);
        var vsSolution = SetUpProjectGuid(ProjectName, ProjectGuid);

        _ = CreateTestSubject(vsSolution, dte);

        serviceProvider.DidNotReceiveWithAnyArgs().GetService(default);
    }

    [TestMethod]
    public void GetDocumentProjectInfoAsync_RunsOnUIThread()
    {
        var dte = SetUpCorrectDte(ProjectName);
        var vsSolution = SetUpProjectGuid(ProjectName, ProjectGuid);
        var testSubject = CreateTestSubject(vsSolution, dte);

        testSubject.GetDocumentProjectInfo(FilePath);

        Received.InOrder(() =>
        {
            threadHandling.RunOnUIThread(Arg.Any<Action>());
            _ = dte.Solution;
            vsSolution.GetGuidOfProjectFile(Arg.Any<string>());
        });
    }

    [TestMethod]
    public void GetDocumentProjectInfoAsync_NoProjectItem_ReturnsNone()
    {
        var dte = SetUpDte(SetUpSolution(null));
        var vsSolution = Substitute.For<IVsSolution5>();
        var testSubject = CreateTestSubject(vsSolution, dte);

        var result = testSubject.GetDocumentProjectInfo(FilePath);

        result.Should().BeEquivalentTo(("{none}", Guid.Empty));
        logger.AssertNoOutputMessages();
        vsSolution.DidNotReceiveWithAnyArgs().GetGuidOfProjectFile(default);
    }

    [TestMethod]
    public void GetDocumentProjectInfoAsync_NoProject_ReturnsNone()
    {
        var dte = SetUpDte(SetUpSolution(SetUpProjectItem(null)));
        var vsSolution = Substitute.For<IVsSolution5>();
        var testSubject = CreateTestSubject(vsSolution, dte);

        var result = testSubject.GetDocumentProjectInfo(FilePath);

        result.Should().BeEquivalentTo(("{none}", Guid.Empty));
        logger.AssertNoOutputMessages();
        vsSolution.DidNotReceiveWithAnyArgs().GetGuidOfProjectFile(default);
    }

    [TestMethod]
    public void GetDocumentProjectInfoAsync_NoProjectNameAndGuid_ReturnsNone()
    {
        var project = Substitute.For<Project>();
        project.Name.Returns((string)null);
        project.FileName.Returns((string)null);
        var dte = SetUpDte(SetUpSolution(SetUpProjectItem(project)));
        var vsSolution = Substitute.For<IVsSolution5>();
        var testSubject = CreateTestSubject(vsSolution, dte);

        var result = testSubject.GetDocumentProjectInfo(FilePath);

        result.Should().BeEquivalentTo(("{none}", Guid.Empty));
        logger.AssertNoOutputMessages();
        vsSolution.DidNotReceiveWithAnyArgs().GetGuidOfProjectFile(default);
    }

    [TestMethod]
    public void GetDocumentProjectInfoAsync_NoProjectNameAndGuid_ReturnsNoneWithGuid()
    {
        var project = Substitute.For<Project>();
        project.Name.Returns((string)null);
        project.FileName.Returns("someprojectfile.csproj");
        var dte = SetUpDte(SetUpSolution(SetUpProjectItem(project)));
        var vsSolution = SetUpProjectGuid("someprojectfile", ProjectGuid);
        var testSubject = CreateTestSubject(vsSolution, dte);

        var result = testSubject.GetDocumentProjectInfo(FilePath);

        result.Should().BeEquivalentTo(("{none}", ProjectGuid));
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void GetDocumentProjectInfoAsync_NoProjectFile_ReturnsNameOnly()
    {
        var project = Substitute.For<Project>();
        project.Name.Returns(ProjectName);
        project.FileName.Returns((string)null);
        var dte = SetUpDte(SetUpSolution(SetUpProjectItem(project)));
        var vsSolution = Substitute.For<IVsSolution5>();
        var testSubject = CreateTestSubject(vsSolution, dte);

        var result = testSubject.GetDocumentProjectInfo(FilePath);

        result.Should().BeEquivalentTo((ProjectName, Guid.Empty));
        logger.AssertNoOutputMessages();
        vsSolution.DidNotReceiveWithAnyArgs().GetGuidOfProjectFile(default);
    }

    [TestMethod]
    public void GetDocumentProjectInfoAsync_CorrectProject_ReturnsNameAndGuid()
    {
        var dte = SetUpCorrectDte(ProjectName);
        var vsSolution = SetUpProjectGuid(ProjectName, ProjectGuid);
        var testSubject = CreateTestSubject(vsSolution, dte);

        var result = testSubject.GetDocumentProjectInfo(FilePath);

        result.Should().BeEquivalentTo((ProjectName, ProjectGuid));
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void GetDocumentProjectInfoAsync_ProjectGuidThrows_NonCriticalException_Logs()
    {
        var dte = SetUpCorrectDte(ProjectName);
        var vsSolution = Substitute.For<IVsSolution5>();
        vsSolution
            .GetGuidOfProjectFile(default)
            .ThrowsForAnyArgs(new InvalidOperationException());
        var testSubject = CreateTestSubject(vsSolution, dte);

        var act = () => testSubject.GetDocumentProjectInfo(FilePath);

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("Failed to calculate project guid of file");
    }

    [TestMethod]
    public void GetDocumentProjectInfoAsync_ProjectGuidThrows_CriticalException_DoesNotCatch()
    {
        var dte = SetUpCorrectDte(ProjectName);
        var vsSolution = Substitute.For<IVsSolution5>();
        vsSolution
            .GetGuidOfProjectFile(default)
            .ThrowsForAnyArgs(new DivideByZeroException());
        var testSubject = CreateTestSubject(vsSolution, dte);

        var act = () => testSubject.GetDocumentProjectInfo(FilePath);

        act.Should().Throw<DivideByZeroException>();
    }

    private IVsProjectInfoProvider CreateTestSubject(
        IVsSolution5 vsSolution,
        DTE2 dte)
    {
        serviceProvider.GetService(typeof(SDTE)).Returns(dte);
        serviceProvider.GetService(typeof(SVsSolution)).Returns(vsSolution);

        return new VsProjectInfoProvider(serviceProvider, logger, threadHandling);
    }

    private static DTE2 SetUpCorrectDte(string projectName) => SetUpDte(SetUpSolution(SetUpProjectItem(SetUpProject(projectName))));

    private static Project SetUpProject(string projectName)
    {
        var mockProject = Substitute.For<Project>();
        mockProject.Name.Returns(projectName);
        mockProject.FileName.Returns($"{projectName}.csproj");
        return mockProject;
    }

    private static ProjectItem SetUpProjectItem(Project project)
    {
        var mockProjectItem = Substitute.For<ProjectItem>();
        mockProjectItem.ContainingProject.Returns(project);
        return mockProjectItem;
    }

    private static Solution SetUpSolution(ProjectItem projectItem)
    {
        var mockSolution = Substitute.For<Solution>();
        mockSolution
            .FindProjectItem(Arg.Any<string>())
            .Returns(projectItem);
        return mockSolution;
    }

    private static DTE2 SetUpDte(Solution solution)
    {
        var mockDTE = Substitute.For<DTE2>();
        mockDTE.Solution.Returns(solution);
        return mockDTE;
    }

    private static IVsSolution5 SetUpProjectGuid(string projectName, Guid projectGuid)
    {
        var mockVsSolution = Substitute.For<IVsSolution5>();
        mockVsSolution.GetGuidOfProjectFile($"{projectName}.csproj")
            .Returns(projectGuid);
        return mockVsSolution;
    }
}
