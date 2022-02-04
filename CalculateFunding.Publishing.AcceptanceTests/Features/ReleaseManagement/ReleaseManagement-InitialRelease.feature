Feature: ReleaseManagement-InitialRelease

Release providers to one or more channels - no providers have existing releases

@releasemanagement
Scenario Outline: Initial release of providers into channels
	Given funding is released for providers
		| ProviderId |
		| 10071688   |
	And release management repo has prereq data populated
	And funding is released for channels
		| Statement |
		| Payment   |
	And the following specification exists
		| Field                | Value                |
		| Id                   | <SpecificationId>    |
		| Name                 | <SpecificationName>  |
		| IsSelectedForFunding | true                 |
		| ProviderVersionId    | <ProviderVersionId>  |
		| ProviderSnapshotId   | <ProviderSnapshotId> |
		| FundingStreamId      | <FundingStreamId>    |
		| FundingPeriodId      | <FundingPeriodId>    |
		| TemplateVersion      | 1.2                  |
	And the following specification exists in release management
		| Field             | Value               |
		| SpecificationId   | <SpecificationId>   |
		| SpecificationName | <SpecificationName> |
		| FundingStreamId   | <FundingStreamId>   |
		| FundingPeriodId   | <FundingPeriodId>   |
	And published provider '10071688' exists for funding string '<FundingStreamId>' in period '<FundingPeriodId>' in cosmos from json
	And a funding configuration exists for funding stream '<FundingStreamId>' in funding period '<FundingPeriodId>' in resources
	And the funding period exists in the policies service
	| Field     | Value               |
	| Period    | 2122                |
	| Type      | AY                  |
	| ID        | <FundingPeriodId>   |
	| StartDate | 2021-09-01 00:00:00 |
	| EndDate   | 2022-08-31 23:59:59 |
	And the provider version '<ProviderVersionId>' exists in the provider service for '<ProviderVersionId>'
	And all providers in provider version '<ProviderVersionId>' are in scope
	And the payment organisations are available for provider snapshot '<ProviderSnapshotId>' from FDZ
	And the following job is requested to be queued for the current specification
	| Field | Value   |
	| JobId | <JobId> |
	And the job is submitted to the job service
	When funding is released to channels for selected providers
	| Field         | Value         |
	| CorrelationId | Corr          |
	| AuthorName    | Releaser Name |
	| AuthorId      | Rel1          |
	Then there is a released provider record in the release management repository
	| Field              | Value             |
	| ReleasedProviderId | 1                 |
	| SpecificationId    | <SpecificationId> |
	| ProviderId         | 10071688          |

Examples:
	| FundingStreamId | FundingPeriodId | SpecificationId                      | Specification Name | ProviderVersionId | ProviderSnapshotId |
	| PSG             | AY-2122         | 3812005f-13b3-4d00-a118-d6cb0e2b2402 | PE and Sport Grant | PSG-2021-10-11-76 | 76                 |