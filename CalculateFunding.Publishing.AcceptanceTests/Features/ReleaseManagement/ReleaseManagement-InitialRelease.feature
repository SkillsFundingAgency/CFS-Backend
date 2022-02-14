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
	And the current date and time is '<CurrentDateTime>'
	When funding is released to channels for selected providers
		| Field         | Value        |
		| CorrelationId | Corr         |
		| AuthorName    | <AuthorName> |
		| AuthorId      | <AuthorId>   |
	Then there is a released provider record in the release management repository
		| Field              | Value             |
		| ReleasedProviderId | 1                 |
		| SpecificationId    | <SpecificationId> |
		| ProviderId         | 10071688          |
	And there is a released provider version record created in the release management repository
		| Field                     | Value                    |
		| ReleasedProviderVersionId | 1                        |
		| ReleasedProviderId        | 1                        |
		| MajorVersion              | 1                        |
		| FundingId                 | PSG-AY-2122-10071688-1_0 |
		| TotalFunding              | 17780                    |
		| CoreProviderVersionId     | <ProviderVersionId>      |
	And there is a released provider version channel record created in the release management repository
		| Field                            | Value             |
		| ReleasedProviderVersionChannelId | 1                 |
		| ReleasedProviderVersionId        | 1                 |
		| ChannelId                        | 3                 |
		| StatusChangedDate                | <CurrentDateTime> |
		| AuthorId                         | <AuthorId>        |
		| AuthorName                       | <AuthorName>      |
	And there is content blob created for the funding group with ID 'PSG-AY-2122-Information-LocalAuthority-212-1_0' in the channel 'Statement'
	And there is content blob created for the funding group with ID 'PSG-AY-2122-Payment-LocalAuthority-10004002-1_0' in the channel 'Statement'
	And there is content blob created for the funding group with ID 'PSG-AY-2122-Payment-LocalAuthority-10004002-1_0' in the channel 'Payment'
	And there is content blob created for the released published provider with ID 'PSG-AY-2122-10071688-1_0'
	And there is content blob created for the released provider with ID 'PSG-AY-2122-10071688-1_0' in channel 'Payment'
	And there is content blob created for the released provider with ID 'PSG-AY-2122-10071688-1_0' in channel 'Statement'

Examples:
	| FundingStreamId | FundingPeriodId | SpecificationId                      | Specification Name | ProviderVersionId | ProviderSnapshotId | CurrentDateTime     | AuthorId | AuthorName  |
	| PSG             | AY-2122         | 3812005f-13b3-4d00-a118-d6cb0e2b2402 | PE and Sport Grant | PSG-2021-10-11-76 | 76                 | 2022-02-10 14:18:00 | AuthId   | Author Name |