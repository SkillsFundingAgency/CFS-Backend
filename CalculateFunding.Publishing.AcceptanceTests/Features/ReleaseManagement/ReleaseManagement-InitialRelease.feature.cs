﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (https://www.specflow.org/).
//      SpecFlow Version:3.9.0.0
//      SpecFlow Generator Version:3.9.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace CalculateFunding.Publishing.AcceptanceTests.Features.ReleaseManagement
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute()]
    public partial class ReleaseManagement_InitialReleaseFeature
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
        private Microsoft.VisualStudio.TestTools.UnitTesting.TestContext _testContext;
        
        private string[] _featureTags = ((string[])(null));
        
#line 1 "ReleaseManagement-InitialRelease.feature"
#line hidden
        
        public virtual Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext
        {
            get
            {
                return this._testContext;
            }
            set
            {
                this._testContext = value;
            }
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute()]
        public static void FeatureSetup(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext testContext)
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Features/ReleaseManagement", "ReleaseManagement-InitialRelease", "Release providers to one or more channels - no providers have existing releases", ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute()]
        public static void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute()]
        public virtual void TestInitialize()
        {
            if (((testRunner.FeatureContext != null) 
                        && (testRunner.FeatureContext.FeatureInfo.Title != "ReleaseManagement-InitialRelease")))
            {
                global::CalculateFunding.Publishing.AcceptanceTests.Features.ReleaseManagement.ReleaseManagement_InitialReleaseFeature.FeatureSetup(null);
            }
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute()]
        public virtual void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<Microsoft.VisualStudio.TestTools.UnitTesting.TestContext>(_testContext);
        }
        
        public virtual void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        public virtual void InitialReleaseOfProvidersIntoChannels(string fundingStreamId, string fundingPeriodId, string specificationId, string specificationName, string providerVersionId, string providerSnapshotId, string currentDateTime, string authorId, string authorName, string[] exampleTags)
        {
            string[] @__tags = new string[] {
                    "releasemanagement"};
            if ((exampleTags != null))
            {
                @__tags = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(@__tags, exampleTags));
            }
            string[] tagsOfScenario = @__tags;
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            argumentsOfScenario.Add("FundingStreamId", fundingStreamId);
            argumentsOfScenario.Add("FundingPeriodId", fundingPeriodId);
            argumentsOfScenario.Add("SpecificationId", specificationId);
            argumentsOfScenario.Add("Specification Name", specificationName);
            argumentsOfScenario.Add("ProviderVersionId", providerVersionId);
            argumentsOfScenario.Add("ProviderSnapshotId", providerSnapshotId);
            argumentsOfScenario.Add("CurrentDateTime", currentDateTime);
            argumentsOfScenario.Add("AuthorId", authorId);
            argumentsOfScenario.Add("AuthorName", authorName);
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Initial release of providers into channels", null, tagsOfScenario, argumentsOfScenario, this._featureTags);
#line 6
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
                TechTalk.SpecFlow.Table table1204 = new TechTalk.SpecFlow.Table(new string[] {
                            "ProviderId"});
                table1204.AddRow(new string[] {
                            "10071688"});
#line 7
 testRunner.Given("funding is released for providers", ((string)(null)), table1204, "Given ");
#line hidden
#line 10
 testRunner.And("release management repo has prereq data populated", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
                TechTalk.SpecFlow.Table table1205 = new TechTalk.SpecFlow.Table(new string[] {
                            "Statement"});
                table1205.AddRow(new string[] {
                            "Payment"});
#line 11
 testRunner.And("funding is released for channels", ((string)(null)), table1205, "And ");
#line hidden
                TechTalk.SpecFlow.Table table1206 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table1206.AddRow(new string[] {
                            "Id",
                            string.Format("{0}", specificationId)});
                table1206.AddRow(new string[] {
                            "Name",
                            "<SpecificationName>"});
                table1206.AddRow(new string[] {
                            "IsSelectedForFunding",
                            "true"});
                table1206.AddRow(new string[] {
                            "ProviderVersionId",
                            string.Format("{0}", providerVersionId)});
                table1206.AddRow(new string[] {
                            "ProviderSnapshotId",
                            string.Format("{0}", providerSnapshotId)});
                table1206.AddRow(new string[] {
                            "FundingStreamId",
                            string.Format("{0}", fundingStreamId)});
                table1206.AddRow(new string[] {
                            "FundingPeriodId",
                            string.Format("{0}", fundingPeriodId)});
                table1206.AddRow(new string[] {
                            "TemplateVersion",
                            "1.2"});
#line 14
 testRunner.And("the following specification exists", ((string)(null)), table1206, "And ");
#line hidden
                TechTalk.SpecFlow.Table table1207 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table1207.AddRow(new string[] {
                            "SpecificationId",
                            string.Format("{0}", specificationId)});
                table1207.AddRow(new string[] {
                            "SpecificationName",
                            "<SpecificationName>"});
                table1207.AddRow(new string[] {
                            "FundingStreamId",
                            string.Format("{0}", fundingStreamId)});
                table1207.AddRow(new string[] {
                            "FundingPeriodId",
                            string.Format("{0}", fundingPeriodId)});
#line 24
 testRunner.And("the following specification exists in release management", ((string)(null)), table1207, "And ");
#line hidden
#line 30
 testRunner.And(string.Format("published provider \'10071688\' exists for funding string \'{0}\' in period \'{1}\' in " +
                            "cosmos from json", fundingStreamId, fundingPeriodId), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 31
 testRunner.And(string.Format("a funding configuration exists for funding stream \'{0}\' in funding period \'{1}\' i" +
                            "n resources", fundingStreamId, fundingPeriodId), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
                TechTalk.SpecFlow.Table table1208 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table1208.AddRow(new string[] {
                            "Period",
                            "2122"});
                table1208.AddRow(new string[] {
                            "Type",
                            "AY"});
                table1208.AddRow(new string[] {
                            "ID",
                            string.Format("{0}", fundingPeriodId)});
                table1208.AddRow(new string[] {
                            "StartDate",
                            "2021-09-01 00:00:00"});
                table1208.AddRow(new string[] {
                            "EndDate",
                            "2022-08-31 23:59:59"});
#line 32
 testRunner.And("the funding period exists in the policies service", ((string)(null)), table1208, "And ");
#line hidden
#line 39
 testRunner.And(string.Format("the provider version \'{0}\' exists in the provider service for \'{0}\'", providerVersionId), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 40
 testRunner.And(string.Format("all providers in provider version \'{0}\' are in scope", providerVersionId), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 41
 testRunner.And(string.Format("the payment organisations are available for provider snapshot \'{0}\' from FDZ", providerSnapshotId), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
                TechTalk.SpecFlow.Table table1209 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table1209.AddRow(new string[] {
                            "JobId",
                            "<JobId>"});
#line 42
 testRunner.And("the following job is requested to be queued for the current specification", ((string)(null)), table1209, "And ");
#line hidden
#line 45
 testRunner.And("the job is submitted to the job service", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 46
 testRunner.And(string.Format("the current date and time is \'{0}\'", currentDateTime), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
                TechTalk.SpecFlow.Table table1210 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table1210.AddRow(new string[] {
                            "CorrelationId",
                            "Corr"});
                table1210.AddRow(new string[] {
                            "AuthorName",
                            string.Format("{0}", authorName)});
                table1210.AddRow(new string[] {
                            "AuthorId",
                            string.Format("{0}", authorId)});
#line 47
 testRunner.When("funding is released to channels for selected providers", ((string)(null)), table1210, "When ");
#line hidden
                TechTalk.SpecFlow.Table table1211 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table1211.AddRow(new string[] {
                            "ReleasedProviderId",
                            "1"});
                table1211.AddRow(new string[] {
                            "SpecificationId",
                            string.Format("{0}", specificationId)});
                table1211.AddRow(new string[] {
                            "ProviderId",
                            "10071688"});
#line 52
 testRunner.Then("there is a released provider record in the release management repository", ((string)(null)), table1211, "Then ");
#line hidden
                TechTalk.SpecFlow.Table table1212 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table1212.AddRow(new string[] {
                            "ReleasedProviderVersionId",
                            "1"});
                table1212.AddRow(new string[] {
                            "ReleasedProviderId",
                            "1"});
                table1212.AddRow(new string[] {
                            "MajorVersion",
                            "1"});
                table1212.AddRow(new string[] {
                            "FundingId",
                            "PSG-AY-2122-10071688-1_0"});
                table1212.AddRow(new string[] {
                            "TotalFunding",
                            "17780"});
                table1212.AddRow(new string[] {
                            "CoreProviderVersionId",
                            string.Format("{0}", providerVersionId)});
#line 57
 testRunner.And("there is a released provider version record created in the release management rep" +
                        "ository", ((string)(null)), table1212, "And ");
#line hidden
                TechTalk.SpecFlow.Table table1213 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table1213.AddRow(new string[] {
                            "ReleasedProviderVersionChannelId",
                            "1"});
                table1213.AddRow(new string[] {
                            "ReleasedProviderVersionId",
                            "1"});
                table1213.AddRow(new string[] {
                            "ChannelId",
                            "3"});
                table1213.AddRow(new string[] {
                            "StatusChangedDate",
                            string.Format("{0}", currentDateTime)});
                table1213.AddRow(new string[] {
                            "AuthorId",
                            string.Format("{0}", authorId)});
                table1213.AddRow(new string[] {
                            "AuthorName",
                            string.Format("{0}", authorName)});
#line 65
 testRunner.And("there is a released provider version channel record created in the release manage" +
                        "ment repository", ((string)(null)), table1213, "And ");
#line hidden
#line 73
 testRunner.And("there is content blob created for the funding group with ID \'PSG-AY-2122-Informat" +
                        "ion-LocalAuthority-212-1_0\' in the channel \'Statement\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 74
 testRunner.And("there is content blob created for the funding group with ID \'PSG-AY-2122-Payment-" +
                        "LocalAuthority-10004002-1_0\' in the channel \'Statement\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 75
 testRunner.And("there is content blob created for the funding group with ID \'PSG-AY-2122-Payment-" +
                        "LocalAuthority-10004002-1_0\' in the channel \'Payment\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 76
 testRunner.And("there is content blob created for the released published provider with ID \'PSG-AY" +
                        "-2122-10071688-1_0\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 77
 testRunner.And("there is content blob created for the released provider with ID \'PSG-AY-2122-1007" +
                        "1688-1_0\' in channel \'Payment\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 78
 testRunner.And("there is content blob created for the released provider with ID \'PSG-AY-2122-1007" +
                        "1688-1_0\' in channel \'Statement\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute()]
        [Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute("Initial release of providers into channels: PSG")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("FeatureTitle", "ReleaseManagement-InitialRelease")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute("releasemanagement")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("VariantName", "PSG")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:FundingStreamId", "PSG")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:FundingPeriodId", "AY-2122")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:SpecificationId", "3812005f-13b3-4d00-a118-d6cb0e2b2402")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:Specification Name", "PE and Sport Grant")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:ProviderVersionId", "PSG-2021-10-11-76")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:ProviderSnapshotId", "76")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:CurrentDateTime", "2022-02-10 14:18:00")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:AuthorId", "AuthId")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:AuthorName", "Author Name")]
        public virtual void InitialReleaseOfProvidersIntoChannels_PSG()
        {
#line 6
this.InitialReleaseOfProvidersIntoChannels("PSG", "AY-2122", "3812005f-13b3-4d00-a118-d6cb0e2b2402", "PE and Sport Grant", "PSG-2021-10-11-76", "76", "2022-02-10 14:18:00", "AuthId", "Author Name", ((string[])(null)));
#line hidden
        }
    }
}
#pragma warning restore
#endregion
