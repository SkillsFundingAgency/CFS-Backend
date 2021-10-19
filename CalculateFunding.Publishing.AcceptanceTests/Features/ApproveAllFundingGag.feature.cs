﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (https://www.specflow.org/).
//      SpecFlow Version:3.6.0.0
//      SpecFlow Generator Version:3.6.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace CalculateFunding.Publishing.AcceptanceTests.Features
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.6.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute()]
    public partial class ApproveAllFundingGagFeature
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
        private Microsoft.VisualStudio.TestTools.UnitTesting.TestContext _testContext;
        
        private string[] _featureTags = ((string[])(null));
        
#line 1 "ApproveAllFundingGag.feature"
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
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Features", "ApproveAllFundingGag", "\tIn order to approve funding for GAG\r\n\tAs a funding approver\r\n\tI want to approve " +
                    "funding for all providers within a specification", ProgrammingLanguage.CSharp, ((string[])(null)));
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
                        && (testRunner.FeatureContext.FeatureInfo.Title != "ApproveAllFundingGag")))
            {
                global::CalculateFunding.Publishing.AcceptanceTests.Features.ApproveAllFundingGagFeature.FeatureSetup(null);
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
        
        public virtual void SuccessfulApproveOfFunding(string fundingStreamId, string fundingPeriodId, string fundingPeriodName, string templateVersion, string providerVersionId, string[] exampleTags)
        {
            string[] tagsOfScenario = exampleTags;
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            argumentsOfScenario.Add("FundingStreamId", fundingStreamId);
            argumentsOfScenario.Add("FundingPeriodId", fundingPeriodId);
            argumentsOfScenario.Add("FundingPeriodName", fundingPeriodName);
            argumentsOfScenario.Add("TemplateVersion", templateVersion);
            argumentsOfScenario.Add("ProviderVersionId", providerVersionId);
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Successful approve of funding", null, tagsOfScenario, argumentsOfScenario, this._featureTags);
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
                TechTalk.SpecFlow.Table table52 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table52.AddRow(new string[] {
                            "DefaultTemplateVersion",
                            "1.2"});
                table52.AddRow(new string[] {
                            "PaymentOrganisationSource",
                            "PaymentOrganisationFields"});
#line 7
 testRunner.Given(string.Format("a funding configuration exists for funding stream \'{0}\' in funding period \'{1}\'", fundingStreamId, fundingPeriodId), ((string)(null)), table52, "Given ");
#line hidden
                TechTalk.SpecFlow.Table table53 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table53.AddRow(new string[] {
                            "GroupTypeIdentifier",
                            "UKPRN"});
                table53.AddRow(new string[] {
                            "GroupingReason",
                            "Payment"});
                table53.AddRow(new string[] {
                            "GroupTypeClassification",
                            "LegalEntity"});
                table53.AddRow(new string[] {
                            "OrganisationGroupTypeCode",
                            "AcademyTrust"});
#line 11
 testRunner.And("the funding configuration has the following organisation group and provider statu" +
                        "s list \'Open;Open, but proposed to close\'", ((string)(null)), table53, "And ");
#line hidden
#line 17
 testRunner.And("the funding configuration is available in the policies repository", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
                TechTalk.SpecFlow.Table table54 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table54.AddRow(new string[] {
                            "GroupTypeIdentifier",
                            "UKPRN"});
                table54.AddRow(new string[] {
                            "GroupingReason",
                            "Information"});
                table54.AddRow(new string[] {
                            "GroupTypeClassification",
                            "LegalEntity"});
                table54.AddRow(new string[] {
                            "OrganisationGroupTypeCode",
                            "AcademyTrust"});
#line 18
 testRunner.And("the funding configuration has the following organisation group and provider statu" +
                        "s list \'Open;Open, but proposed to close\'", ((string)(null)), table54, "And ");
#line hidden
#line 24
 testRunner.And("the funding configuration is available in the policies repository", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
                TechTalk.SpecFlow.Table table55 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table55.AddRow(new string[] {
                            "GroupTypeIdentifier",
                            "UKPRN"});
                table55.AddRow(new string[] {
                            "GroupingReason",
                            "Indicative"});
                table55.AddRow(new string[] {
                            "GroupTypeClassification",
                            "LegalEntity"});
                table55.AddRow(new string[] {
                            "OrganisationGroupTypeCode",
                            "AcademyTrust"});
#line 25
 testRunner.And("the funding configuration has the following organisation group and provider statu" +
                        "s list \'Proposed to open\'", ((string)(null)), table55, "And ");
#line hidden
#line 31
 testRunner.And("the funding configuration is available in the policies repository", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
                TechTalk.SpecFlow.Table table56 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table56.AddRow(new string[] {
                            "Id",
                            string.Format("{0}", fundingPeriodId)});
                table56.AddRow(new string[] {
                            "Name",
                            string.Format("{0}", fundingPeriodName)});
                table56.AddRow(new string[] {
                            "StartDate",
                            "2019-08-01 00:00:00"});
                table56.AddRow(new string[] {
                            "EndDate",
                            "2020-07-31 00:00:00"});
                table56.AddRow(new string[] {
                            "Period",
                            "2021"});
                table56.AddRow(new string[] {
                            "Type",
                            "AC"});
#line 32
 testRunner.And("the funding period exists in the policies service", ((string)(null)), table56, "And ");
#line hidden
                TechTalk.SpecFlow.Table table57 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table57.AddRow(new string[] {
                            "Id",
                            "specForPublishing"});
                table57.AddRow(new string[] {
                            "Name",
                            "Test Specification for Publishing"});
                table57.AddRow(new string[] {
                            "IsSelectedForFunding",
                            "true"});
                table57.AddRow(new string[] {
                            "ProviderVersionId",
                            string.Format("{0}", providerVersionId)});
#line 40
 testRunner.And("the following specification exists", ((string)(null)), table57, "And ");
#line hidden
#line 46
 testRunner.And(string.Format("the specification has the funding period with id \'{0}\' and name \'{1}\'", fundingPeriodId, fundingPeriodName), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
                TechTalk.SpecFlow.Table table58 = new TechTalk.SpecFlow.Table(new string[] {
                            "Name",
                            "Id"});
                table58.AddRow(new string[] {
                            "GAG",
                            string.Format("{0}", fundingStreamId)});
#line 47
 testRunner.And("the specification has the following funding streams", ((string)(null)), table58, "And ");
#line hidden
                TechTalk.SpecFlow.Table table59 = new TechTalk.SpecFlow.Table(new string[] {
                            "Key",
                            "Value"});
                table59.AddRow(new string[] {
                            string.Format("{0}", fundingStreamId),
                            "1.2"});
#line 50
 testRunner.And("the specification has the following template versions for funding streams", ((string)(null)), table59, "And ");
#line hidden
                TechTalk.SpecFlow.Table table60 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table60.AddRow(new string[] {
                            "StatusChangedDate",
                            "2019-09-27 00:00:00"});
                table60.AddRow(new string[] {
                            "ExternalPublicationDate",
                            "2019-09-28 00:00:00"});
                table60.AddRow(new string[] {
                            "EarliestPaymentAvailableDate",
                            "2019-09-29 00:00:00"});
#line 53
 testRunner.And("the publishing dates for the specifcation are set as following", ((string)(null)), table60, "And ");
#line hidden
                TechTalk.SpecFlow.Table table61 = new TechTalk.SpecFlow.Table(new string[] {
                            "Field",
                            "Value"});
                table61.AddRow(new string[] {
                            "JobDefinitionId",
                            "PublishFundingJob"});
                table61.AddRow(new string[] {
                            "InvokerUserId",
                            "PublishUserId"});
                table61.AddRow(new string[] {
                            "InvokerUserDisplayName",
                            "Invoker User"});
                table61.AddRow(new string[] {
                            "ParentJobId",
                            ""});
#line 58
 testRunner.And("the following job is requested to be queued for the current specification", ((string)(null)), table61, "And ");
#line hidden
#line 64
 testRunner.And("the job is submitted to the job service", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 65
 testRunner.And(string.Format("the provider version \'gag-providers-1_0\' exists in the provider service for \'{0}\'" +
                            "", providerVersionId), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 66
 testRunner.And("template mapping \'GAG-TemplateMapping\' exists", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 67
 testRunner.And("the Published Provider \'GAG-AC-2021-1000000\' has been been previously generated f" +
                        "or the current specification", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 68
 testRunner.And("the Published Provider is available in the repository for this specification", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 69
 testRunner.And(string.Format("the provider with id \'1000000\' should be a scoped provider in the current specifi" +
                            "cation in provider version \'{0}\'", providerVersionId), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 70
 testRunner.And("the Published Provider \'GAG-AC-2021-1000002\' has been been previously generated f" +
                        "or the current specification", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 71
 testRunner.And("the Published Provider is available in the repository for this specification", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 72
 testRunner.And(string.Format("the provider with id \'1000002\' should be a scoped provider in the current specifi" +
                            "cation in provider version \'{0}\'", providerVersionId), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 73
 testRunner.And("calculations \'gag-approve-all-funding-calculations\' exists", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 74
 testRunner.When("funding is approved", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
                TechTalk.SpecFlow.Table table62 = new TechTalk.SpecFlow.Table(new string[] {
                            "PublishedProviderId",
                            "Status"});
                table62.AddRow(new string[] {
                            string.Format("publishedprovider-1000000-{0}-{1}", fundingPeriodId, fundingStreamId),
                            "Approved"});
                table62.AddRow(new string[] {
                            string.Format("publishedprovider-1000002-{0}-{1}", fundingPeriodId, fundingStreamId),
                            "Approved"});
#line 75
 testRunner.Then("the following published provider ids are upserted", ((string)(null)), table62, "Then ");
#line hidden
                TechTalk.SpecFlow.Table table63 = new TechTalk.SpecFlow.Table(new string[] {
                            "ID",
                            "ProviderType",
                            "ProviderSubType",
                            "LocalAuthority",
                            "FundingStatus",
                            "ProviderName",
                            "UKPRN",
                            "FundingValue",
                            "SpecificationId",
                            "FundingStreamId",
                            "FundingPeriodId",
                            "UPIN",
                            "URN",
                            "Errors",
                            "Indicative",
                            "MajorVersion",
                            "MinorVersion"});
                table63.AddRow(new string[] {
                            "GAG-AC-2021-1000002",
                            "Academies",
                            "Academy sponsor led",
                            "West Sussex",
                            "Approved",
                            "Midhurst Rother College",
                            "1000002",
                            "5555790.01",
                            "specForPublishing",
                            string.Format("{0}", fundingStreamId),
                            string.Format("{0}", fundingPeriodId),
                            "118907",
                            "135760",
                            "",
                            "Hide indicative allocations",
                            "2",
                            "1"});
#line 79
 testRunner.And(string.Format("the following published provider search index items is produced for providerid wi" +
                            "th \'{0}\' and \'{1}\'", fundingStreamId, fundingPeriodId), ((string)(null)), table63, "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute()]
        [Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute("Successful approve of funding: GAG")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("FeatureTitle", "ApproveAllFundingGag")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("VariantName", "GAG")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:FundingStreamId", "GAG")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:FundingPeriodId", "AC-2021")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:FundingPeriodName", "Academies Academic Year 2020-21")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:TemplateVersion", "1.2")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("Parameter:ProviderVersionId", "gag-providers-1.0")]
        public virtual void SuccessfulApproveOfFunding_GAG()
        {
#line 6
this.SuccessfulApproveOfFunding("GAG", "AC-2021", "Academies Academic Year 2020-21", "1.2", "gag-providers-1.0", ((string[])(null)));
#line hidden
        }
    }
}
#pragma warning restore
#endregion
