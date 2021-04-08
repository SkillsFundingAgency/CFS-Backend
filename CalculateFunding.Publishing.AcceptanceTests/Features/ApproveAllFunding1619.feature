Feature: ApproveAllFunding1619
	In order to approve funding for 1619
	As a funding approver
	I want to approve funding for all providers within a specification

Scenario Outline: Successful approve of funding
	Given a funding configuration exists for funding stream '<FundingStreamId>' in funding period '<FundingPeriodId>'
		| Field                  | Value |
		| DefaultTemplateVersion | 1.0   |
	And the funding configuration has the following organisation group
		| Field                     | Value          |
		| GroupTypeIdentifier       | UKPRN          |
		| GroupingReason            | Payment        |
		| GroupTypeClassification   | LegalEntity    |
		| OrganisationGroupTypeCode | LocalAuthority |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value                |
		| GroupTypeIdentifier       | LACode               |
		| GroupingReason            | Information          |
		| GroupTypeClassification   | GeographicalBoundary |
		| OrganisationGroupTypeCode | LocalAuthority       |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value                                |
		| GroupTypeIdentifier       | LocalAuthorityClassificationTypeCode |
		| GroupingReason            | Information                          |
		| GroupTypeClassification   | GeographicalBoundary                 |
		| OrganisationGroupTypeCode | LocalGovernmentGroup                 |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value                      |
		| GroupTypeIdentifier       | GovernmentOfficeRegionCode |
		| GroupingReason            | Information                |
		| GroupTypeClassification   | GeographicalBoundary       |
		| OrganisationGroupTypeCode | GovernmentOfficeRegion     |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value                |
		| GroupTypeIdentifier       | CountryCode          |
		| GroupingReason            | Information          |
		| GroupTypeClassification   | GeographicalBoundary |
		| OrganisationGroupTypeCode | Country              |
	And the funding configuration is available in the policies repository
	And the funding period exists in the policies service
		| Field     | Value               |
		| Id        | <FundingPeriodId>   |
		| Name      | <FundingPeriodName> |
		| StartDate | 2019-08-01 00:00:00 |
		| EndDate   | 2020-07-31 00:00:00 |
		| Period    | 2021                |
		| Type      | AS                  |
	And the following specification exists
		| Field                | Value                             |
		| Id                   | specForPublishing                 |
		| Name                 | Test Specification for Publishing |
		| IsSelectedForFunding | true                              |
		| ProviderVersionId    | <ProviderVersionId>               |
	And the specification has the funding period with id '<FundingPeriodId>' and name '<FundingPeriodName>'
	And the specification has the following funding streams
		| Name | Id                |
		| 1619  | <FundingStreamId> |
	And the specification has the following template versions for funding streams
		| Key               | Value |
		| <FundingStreamId> | 1.0   |
	And the publishing dates for the specifcation are set as following
		| Field                        | Value               |
		| StatusChangedDate            | 2019-09-27 00:00:00 |
		| ExternalPublicationDate      | 2019-09-28 00:00:00 |
		| EarliestPaymentAvailableDate | 2019-09-29 00:00:00 |
	And the following job is requested to be queued for the current specification
		| Field                  | Value             |
		| JobDefinitionId        | PublishFundingJob |
		| InvokerUserId          | PublishUserId     |
		| InvokerUserDisplayName | Invoker User      |
		| ParentJobId            |                   |
	And the job is submitted to the job service
	And the provider version '1619-providers-1_0' exists in the provider service for '<ProviderVersionId>'
	And template mapping '1619-TemplateMapping' exists
	And the Published Provider '1619-AS-2021-1000000' has been been previously generated for the current specification
	And the Published Provider is available in the repository for this specification
	And the provider with id '1000000' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the Published Provider '1619-AS-2021-1000002' has been been previously generated for the current specification
	And the Published Provider is available in the repository for this specification
	And the provider with id '1000002' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And calculations '1619-approve-all-funding-calculations' exists
	When funding is approved
	Then the following published provider ids are upserted
		| PublishedProviderId                                           | Status  |
		| publishedprovider-1000000-<FundingPeriodId>-<FundingStreamId> | Approved|
		| publishedprovider-1000002-<FundingPeriodId>-<FundingStreamId> | Approved|
	And the following published provider search index items is produced for providerid with '<FundingStreamId>' and '<FundingPeriodId>'
		| ID                   | ProviderType          | ProviderSubType  | LocalAuthority    | FundingStatus | ProviderName        | UKPRN   | FundingValue | SpecificationId   | FundingStreamId   | FundingPeriodId   | UPIN   | URN     | Errors | Indicative |
		| 1619-AS-2021-1000002 | LA maintained schools | Community school | Local Authority 1 | Approved      | Maintained School 2 | 1000002 | 12000        | specForPublishing | <FundingStreamId> | <FundingPeriodId> | 123456 | 1234567 |        | Hide indicative allocations | 
	Examples:
		| FundingStreamId | FundingPeriodId | FundingPeriodName      | TemplateVersion | ProviderVersionId  |
		| 1619            | AS-2021         | Financial Year 2020-21 | 1.0             | 1619-providers-1.0 |