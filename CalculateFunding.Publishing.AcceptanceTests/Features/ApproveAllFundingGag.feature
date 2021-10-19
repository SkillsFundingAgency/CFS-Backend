Feature: ApproveAllFundingGag
	In order to approve funding for GAG
	As a funding approver
	I want to approve funding for all providers within a specification

Scenario Outline: Successful approve of funding
	Given a funding configuration exists for funding stream '<FundingStreamId>' in funding period '<FundingPeriodId>'
		| Field                     | Value                     |
		| DefaultTemplateVersion    | 1.2                       |
		| PaymentOrganisationSource | PaymentOrganisationFields |
	And the funding configuration has the following organisation group and provider status list 'Open;Open, but proposed to close'
		| Field                     | Value        |
		| GroupTypeIdentifier       | UKPRN        |
		| GroupingReason            | Payment      |
		| GroupTypeClassification   | LegalEntity  |
		| OrganisationGroupTypeCode | AcademyTrust |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group and provider status list 'Open;Open, but proposed to close'
		| Field                     | Value        |
		| GroupTypeIdentifier       | UKPRN        |
		| GroupingReason            | Information  |
		| GroupTypeClassification   | LegalEntity  |
		| OrganisationGroupTypeCode | AcademyTrust |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group and provider status list 'Proposed to open'
		| Field                     | Value        |
		| GroupTypeIdentifier       | UKPRN        |
		| GroupingReason            | Indicative   |
		| GroupTypeClassification   | LegalEntity  |
		| OrganisationGroupTypeCode | AcademyTrust |
	And the funding configuration is available in the policies repository
	And the funding period exists in the policies service
		| Field     | Value               |
		| Id        | <FundingPeriodId>   |
		| Name      | <FundingPeriodName> |
		| StartDate | 2019-08-01 00:00:00 |
		| EndDate   | 2020-07-31 00:00:00 |
		| Period    | 2021                |
		| Type      | AC                  |
	And the following specification exists
		| Field                | Value                             |
		| Id                   | specForPublishing                 |
		| Name                 | Test Specification for Publishing |
		| IsSelectedForFunding | true                              |
		| ProviderVersionId    | <ProviderVersionId>               |
	And the specification has the funding period with id '<FundingPeriodId>' and name '<FundingPeriodName>'
	And the specification has the following funding streams
		| Name | Id                |
		| GAG  | <FundingStreamId> |
	And the specification has the following template versions for funding streams
		| Key               | Value |
		| <FundingStreamId> | 1.2   |
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
	And the provider version 'gag-providers-1_0' exists in the provider service for '<ProviderVersionId>'
	And template mapping 'GAG-TemplateMapping' exists
	And the Published Provider 'GAG-AC-2021-1000000' has been been previously generated for the current specification
	And the Published Provider is available in the repository for this specification
	And the provider with id '1000000' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the Published Provider 'GAG-AC-2021-1000002' has been been previously generated for the current specification
	And the Published Provider is available in the repository for this specification
	And the provider with id '1000002' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And calculations 'gag-approve-all-funding-calculations' exists
	When funding is approved
	Then the following published provider ids are upserted
		| PublishedProviderId                                           | Status   |
		| publishedprovider-1000000-<FundingPeriodId>-<FundingStreamId> | Approved |
		| publishedprovider-1000002-<FundingPeriodId>-<FundingStreamId> | Approved |
	And the following published provider search index items is produced for providerid with '<FundingStreamId>' and '<FundingPeriodId>'
		| ID                  | ProviderType | ProviderSubType     | LocalAuthority | FundingStatus | ProviderName            | UKPRN   | FundingValue | SpecificationId   | FundingStreamId   | FundingPeriodId   | UPIN   | URN    | Errors | Indicative                  | MajorVersion	| MinorVersion	|
		| GAG-AC-2021-1000002 | Academies    | Academy sponsor led | West Sussex    | Approved      | Midhurst Rother College | 1000002 | 5555790.01   | specForPublishing | <FundingStreamId> | <FundingPeriodId> | 118907 | 135760 |        | Hide indicative allocations | 2				| 1				|

	Examples:
		| FundingStreamId | FundingPeriodId | FundingPeriodName               | TemplateVersion | ProviderVersionId |
		| GAG             | AC-2021         | Academies Academic Year 2020-21 | 1.2             | gag-providers-1.0 |