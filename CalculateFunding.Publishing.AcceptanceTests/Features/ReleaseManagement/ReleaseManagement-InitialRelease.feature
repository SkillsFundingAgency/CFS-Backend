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
		| Field         | Value           |
		| CorrelationId | <CorrelationId> |
		| AuthorName    | <AuthorName>    |
		| AuthorId      | <AuthorId>      |

	Then there is a released provider record in the release management repository
		| Field              | Value             |
		| ReleasedProviderId | 1                 |
		| SpecificationId    | <SpecificationId> |
		| ProviderId         | 10071688          |
	And there is a total of '1' released provider records in the release management repoistory

	And there is a released provider version record created in the release management repository
		| Field                     | Value                    |
		| ReleasedProviderVersionId | 1                        |
		| ReleasedProviderId        | 1                        |
		| MajorVersion              | 1                        |
		| FundingId                 | PSG-AY-2122-10071688-1_0 |
		| TotalFunding              | 17780                    |
		| CoreProviderVersionId     | <ProviderVersionId>      |
	And there is a total of '1' released provider version records in the release management repoistory

	And there is a released provider version channel record created in the release management repository
		| Field                            | Value             |
		| ReleasedProviderVersionChannelId | 1                 |
		| ReleasedProviderVersionId        | 1                 |
		| Channel                          | Statement         |
		| StatusChangedDate                | <CurrentDateTime> |
		| AuthorId                         | <AuthorId>        |
		| AuthorName                       | <AuthorName>      |
	And there is a released provider version channel record created in the release management repository
		| Field                            | Value             |
		| ReleasedProviderVersionChannelId | 2                 |
		| ReleasedProviderVersionId        | 1                 |
		| Channel                          | Payment           |
		| StatusChangedDate                | <CurrentDateTime> |
		| AuthorId                         | <AuthorId>        |
		| AuthorName                       | <AuthorName>      |
	And there are a total of '2' released provider version channel records created in the release management repository

	And there is a released provider channel variation created in the release management repository
		| Field                                    | Value          |
		| ReleasedProviderChannelVariationReasonId | 1              |
		| VariationReason                          | FundingUpdated |
		| ReleasedProviderVersionChannelId         | 1              |
	And there is a released provider channel variation created in the release management repository
		| Field                                    | Value            |
		| ReleasedProviderChannelVariationReasonId | 2                |
		| VariationReason                          | ProfilingUpdated |
		| ReleasedProviderVersionChannelId         | 1                |
	And there is a released provider channel variation created in the release management repository
		| Field                                    | Value          |
		| ReleasedProviderChannelVariationReasonId | 3              |
		| VariationReason                          | FundingUpdated |
		| ReleasedProviderVersionChannelId         | 2              |
	And there is a released provider channel variation created in the release management repository
		| Field                                    | Value            |
		| ReleasedProviderChannelVariationReasonId | 4                |
		| VariationReason                          | ProfilingUpdated |
		| ReleasedProviderVersionChannelId         | 2                |
	And there are a total of '4' released provider version channel variation reason records created in the release management repository

	And there is a funding group created in the release management repository
		| Field                               | Value                             |
		| FundingGroupId                      | 1                                 |
		| SpecificationId                     | <SpecificationId>                 |
		| Channel                             | Statement                         |
		| GroupingReason                      | Payment                           |
		| OrganisationGroupTypeCode           | LocalAuthority                    |
		| OrganisationGroupTypeIdentifier     | UKPRN                             |
		| OrganisationGroupIdentifierValue    | 10004002                          |
		| OrganisationGroupName               | WANDSWORTH LONDON BOROUGH COUNCIL |
		| OrganisationGroupSearchableName     | WANDSWORTH_LONDON_BOROUGH_COUNCIL |
		| OrganisationGroupTypeClassification | LegalEntity                       |
	And there is a funding group created in the release management repository
		| Field                               | Value                |
		| FundingGroupId                      | 2                    |
		| SpecificationId                     | <SpecificationId>    |
		| Channel                             | Statement            |
		| GroupingReason                      | Information          |
		| OrganisationGroupTypeCode           | LocalAuthority       |
		| OrganisationGroupTypeIdentifier     | LACode               |
		| OrganisationGroupIdentifierValue    | 212                  |
		| OrganisationGroupName               | Wandsworth           |
		| OrganisationGroupSearchableName     | Wandsworth           |
		| OrganisationGroupTypeClassification | GeographicalBoundary |
	And there is a funding group created in the release management repository
		| Field                               | Value                             |
		| FundingGroupId                      | 3                                 |
		| SpecificationId                     | <SpecificationId>                 |
		| Channel                             | Payment                           |
		| GroupingReason                      | Payment                           |
		| OrganisationGroupTypeCode           | LocalAuthority                    |
		| OrganisationGroupTypeIdentifier     | UKPRN                             |
		| OrganisationGroupIdentifierValue    | 10004002                          |
		| OrganisationGroupName               | WANDSWORTH LONDON BOROUGH COUNCIL |
		| OrganisationGroupSearchableName     | WANDSWORTH_LONDON_BOROUGH_COUNCIL |
		| OrganisationGroupTypeClassification | LegalEntity                       |
	And there are a total of '3' funding group records created in the release management repository

	And there is a funding group version created in the release management repository
		| Field                        | Value                                           |
		| FundingGroupVersionId        | 1                                               |
		| FundingGroupId               | 1                                               |
		| Channel                      | Statement                                       |
		| GroupingReason               | Payment                                         |
		| StatusChangedDate            | <CurrentDateTime>                               |
		| MajorVersion                 | 1                                               |
		| MinorVersion                 | 0                                               |
		| TemplateVersion              | 1.2                                             |
		| SchemaVersion                | 1.2                                             |
		| JobId                        | <JobId>                                         |
		| CorrelationId                | <CorrelationId>                                 |
		| FundingStreamId              | 1                                               |
		| FundingPeriodId              | 1                                               |
		| FundingId                    | PSG-AY-2122-Payment-LocalAuthority-10004002-1_0 |
		| TotalFunding                 | 17780                                           |
		| ExternalPublicationDate      | <CurrentDateTime>                               |
		| EarliestPaymentAvailableDate | <CurrentDateTime>                               |
	And there is a funding group version created in the release management repository
		| Field                        | Value                                          |
		| FundingGroupVersionId        | 2                                              |
		| FundingGroupId               | 2                                              |
		| Channel                      | Statement                                      |
		| GroupingReason               | Information                                    |
		| StatusChangedDate            | <CurrentDateTime>                              |
		| MajorVersion                 | 1                                              |
		| MinorVersion                 | 0                                              |
		| TemplateVersion              | 1.2                                            |
		| SchemaVersion                | 1.2                                            |
		| JobId                        | <JobId>                                        |
		| CorrelationId                | <CorrelationId>                                |
		| FundingStreamId              | 1                                              |
		| FundingPeriodId              | 1                                              |
		| FundingId                    | PSG-AY-2122-Information-LocalAuthority-212-1_0 |
		| TotalFunding                 | 17780                                          |
		| ExternalPublicationDate      | <CurrentDateTime>                              |
		| EarliestPaymentAvailableDate | <CurrentDateTime>                              |
	And there is a funding group version created in the release management repository
		| Field                        | Value                                           |
		| FundingGroupVersionId        | 3                                               |
		| FundingGroupId               | 3                                               |
		| Channel                      | Payment                                         |
		| GroupingReason               | Payment                                         |
		| StatusChangedDate            | <CurrentDateTime>                               |
		| MajorVersion                 | 1                                               |
		| MinorVersion                 | 0                                               |
		| TemplateVersion              | 1.2                                             |
		| SchemaVersion                | 1.2                                             |
		| JobId                        | <JobId>                                         |
		| CorrelationId                | <CorrelationId>                                 |
		| FundingStreamId              | 1                                               |
		| FundingPeriodId              | 1                                               |
		| FundingId                    | PSG-AY-2122-Payment-LocalAuthority-10004002-1_0 |
		| TotalFunding                 | 17780                                           |
		| ExternalPublicationDate      | <CurrentDateTime>                               |
		| EarliestPaymentAvailableDate | <CurrentDateTime>                               |
	And there are a total of '3' funding group version records created in the release management repository

	And there is a funding group variation reason created in the release management repository
		| Field                                | Value          |
		| FundingGroupVersionVariationReasonId | 1              |
		| FundingGroupVersionId                | 1              |
		| VariationReason                      | FundingUpdated |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value            |
		| FundingGroupVersionVariationReasonId | 2                |
		| FundingGroupVersionId                | 1                |
		| VariationReason                      | ProfilingUpdated |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value          |
		| FundingGroupVersionVariationReasonId | 3              |
		| FundingGroupVersionId                | 2              |
		| VariationReason                      | FundingUpdated |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value            |
		| FundingGroupVersionVariationReasonId | 4                |
		| FundingGroupVersionId                | 2                |
		| VariationReason                      | ProfilingUpdated |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value          |
		| FundingGroupVersionVariationReasonId | 5              |
		| FundingGroupVersionId                | 3              |
		| VariationReason                      | FundingUpdated |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value            |
		| FundingGroupVersionVariationReasonId | 6                |
		| FundingGroupVersionId                | 3                |
		| VariationReason                      | ProfilingUpdated |
	And there are a total of '6' funding group version variation reason records created in the release management repository

	And there is the provider version associated with the funding group version in the release management repository
		| Field                            | Value |
		| FundingGroupProviderId           | 1     |
		| FundingGroupVersionId            | 1     |
		| ReleasedProviderVersionChannelId | 1     |
	And there is the provider version associated with the funding group version in the release management repository
		| Field                            | Value |
		| FundingGroupProviderId           | 2     |
		| FundingGroupVersionId            | 2     |
		| ReleasedProviderVersionChannelId | 1     |
	And there is the provider version associated with the funding group version in the release management repository
		| Field                            | Value |
		| FundingGroupProviderId           | 3     |
		| FundingGroupVersionId            | 3     |
		| ReleasedProviderVersionChannelId | 2     |
	And there are a total of '3' funding group providers created in the release management repository

	And there is content blob created for the funding group with ID 'PSG-AY-2122-Information-LocalAuthority-212-1_0' in the channel 'Statement'
	And there is content blob created for the funding group with ID 'PSG-AY-2122-Payment-LocalAuthority-10004002-1_0' in the channel 'Statement'
	And there is content blob created for the funding group with ID 'PSG-AY-2122-Payment-LocalAuthority-10004002-1_0' in the channel 'Payment'
	And there are '3' files contained in the funding groups blob storage

	And there is content blob created for the released published provider with ID 'PSG-AY-2122-10071688-1_0'
	And there are '1' files contained in the published providers blob storage

	And there is content blob created for the released provider with ID 'PSG-AY-2122-10071688-1_0' in channel 'Payment'
	And there is content blob created for the released provider with ID 'PSG-AY-2122-10071688-1_0' in channel 'Statement'
	And there are '2' files contained in the released providers blob storage

Examples:
	| FundingStreamId | FundingPeriodId | SpecificationId                      | Specification Name | ProviderVersionId | ProviderSnapshotId | CurrentDateTime     | AuthorId | AuthorName  | CorrelationId |
	| PSG             | AY-2122         | 3812005f-13b3-4d00-a118-d6cb0e2b2402 | PE and Sport Grant | PSG-2021-10-11-76 | 76                 | 2022-02-10 14:18:00 | AuthId   | Author Name | Corr          |