Feature: ReleaseManagement-Subsequent release - multiple channels

Release providers to one or more channels - one or more provider already has a version released in more or more channels

@releasemanagement
Scenario Outline: Release a new major version of a provider which has already been released in two channels
	Given funding is released for providers
		| ProviderId |
		| <FundingStreamId>-<FundingPeriodId>-10071690   |
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
	And published provider '10071690' exists for funding stream '<FundingStreamId>' in period '<FundingPeriodId>' in cosmos from json
	And published provider version with major version '2' for provider id '10071690' exists for funding stream '<FundingStreamId>' in period '<FundingPeriodId>' in cosmos from json
	And a funding configuration exists for funding stream '<FundingStreamId>' in funding period '<FundingPeriodId>' in resources in file modifier 'Batch'
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

	And there is a released provider record in the release management repository
		| Field              | Value                                |
		| ReleasedProviderId | 00000000-0000-0000-0000-000000000001 |
		| SpecificationId    | <SpecificationId>                    |
		| ProviderId         | 10071690                             |

	And a released provider version exists in the release management repository
		| Field                     | Value                                |
		| ReleasedProviderVersionId | 00000000-0000-0000-0000-000000000001 |
		| ReleasedProviderId        | 00000000-0000-0000-0000-000000000001 |
		| MajorVersion              | 1                                    |
		| FundingId                 | PSG-AY-2122-10071690-1_0             |
		| TotalFunding              | 16000                                |
		| CoreProviderVersionId     | <ProviderVersionId>                  |
	And a released provider version exists in the release management repository
		| Field                     | Value                                |
		| ReleasedProviderVersionId | 00000000-0000-0000-0000-000000000002 |
		| ReleasedProviderId        | 00000000-0000-0000-0000-000000000001 |
		| MajorVersion              | 2                                    |
		| FundingId                 | PSG-AY-2122-10071690-1_0             |
		| TotalFunding              | 17000                                |
		| CoreProviderVersionId     | <ProviderVersionId>                  |
	And the next released provider version identifier for new records should be 3

	And a released provider version channel record exists in the release management repository
		| Field                            | Value                                |
		| ReleasedProviderVersionChannelId | 00000000-0000-0000-0000-000000000001 |
		| ReleasedProviderVersionId        | 00000000-0000-0000-0000-000000000001 |
		| Channel                          | Statement                            |
		| StatusChangedDate                | <CurrentDateTime>                    |
		| AuthorId                         | <AuthorId>                           |
		| AuthorName                       | <AuthorName>                         |
	And a released provider version channel record exists in the release management repository
		| Field                            | Value                                |
		| ReleasedProviderVersionChannelId | 00000000-0000-0000-0000-000000000002 |
		| ReleasedProviderVersionId        | 00000000-0000-0000-0000-000000000001 |
		| Channel                          | Payment                              |
		| StatusChangedDate                | <CurrentDateTime>                    |
		| AuthorId                         | <AuthorId>                           |
		| AuthorName                       | <AuthorName>                         |
	And a released provider version channel record exists in the release management repository
		| Field                            | Value                                |
		| ReleasedProviderVersionChannelId | 00000000-0000-0000-0000-000000000003 |
		| ReleasedProviderVersionId        | 00000000-0000-0000-0000-000000000002 |
		| Channel                          | Statement                            |
		| StatusChangedDate                | <CurrentDateTime>                    |
		| AuthorId                         | <AuthorId>                           |
		| AuthorName                       | <AuthorName>                         |
	And a released provider version channel record exists in the release management repository
		| Field                            | Value                                |
		| ReleasedProviderVersionChannelId | 00000000-0000-0000-0000-000000000004 |
		| ReleasedProviderVersionId        | 00000000-0000-0000-0000-000000000002 |
		| Channel                          | Payment                              |
		| StatusChangedDate                | <CurrentDateTime>                    |
		| AuthorId                         | <AuthorId>                           |
		| AuthorName                       | <AuthorName>                         |
	And the next released provider version channel identifier for new records should be 5


	And a funding group record exists in the release management repository
		| Field                               | Value                                |
		| FundingGroupId                      | 00000000-0000-0000-0000-000000000001 |
		| SpecificationId                     | <SpecificationId>                    |
		| Channel                             | Statement                            |
		| GroupingReason                      | Payment                              |
		| OrganisationGroupTypeCode           | LocalAuthority                       |
		| OrganisationGroupTypeIdentifier     | UKPRN                                |
		| OrganisationGroupIdentifierValue    | 10004002                             |
		| OrganisationGroupName               | WANDSWORTH LONDON BOROUGH COUNCIL    |
		| OrganisationGroupSearchableName     | WANDSWORTH_LONDON_BOROUGH_COUNCIL    |
		| OrganisationGroupTypeClassification | LegalEntity                          |
	And a funding group record exists in the release management repository
		| Field                               | Value                                |
		| FundingGroupId                      | 00000000-0000-0000-0000-000000000002 |
		| SpecificationId                     | <SpecificationId>                    |
		| Channel                             | Statement                            |
		| GroupingReason                      | Information                          |
		| OrganisationGroupTypeCode           | LocalAuthority                       |
		| OrganisationGroupTypeIdentifier     | LACode                               |
		| OrganisationGroupIdentifierValue    | 212                                  |
		| OrganisationGroupName               | Wandsworth                           |
		| OrganisationGroupSearchableName     | Wandsworth                           |
		| OrganisationGroupTypeClassification | GeographicalBoundary                 |
	And a funding group record exists in the release management repository
		| Field                               | Value                                |
		| FundingGroupId                      | 00000000-0000-0000-0000-000000000003 |
		| SpecificationId                     | <SpecificationId>                    |
		| Channel                             | Payment                              |
		| GroupingReason                      | Payment                              |
		| OrganisationGroupTypeCode           | LocalAuthority                       |
		| OrganisationGroupTypeIdentifier     | UKPRN                                |
		| OrganisationGroupIdentifierValue    | 10004002                             |
		| OrganisationGroupName               | WANDSWORTH LONDON BOROUGH COUNCIL    |
		| OrganisationGroupSearchableName     | WANDSWORTH_LONDON_BOROUGH_COUNCIL    |
		| OrganisationGroupTypeClassification | LegalEntity                          |
	And a total of '3' funding group records exist in the release management repository

	And a funding group version exists in the release management repository
		| Field                        | Value                                           |
		| FundingGroupVersionId        | 00000000-0000-0000-0000-000000000001            |
		| FundingGroupId               | 00000000-0000-0000-0000-000000000001            |
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
	And a funding group version exists in the release management repository
		| Field                        | Value                                          |
		| FundingGroupVersionId        | 00000000-0000-0000-0000-000000000002           |
		| FundingGroupId               | 00000000-0000-0000-0000-000000000002           |
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
	And a funding group version exists in the release management repository
		| Field                        | Value                                           |
		| FundingGroupVersionId        | 00000000-0000-0000-0000-000000000003            |
		| FundingGroupId               | 00000000-0000-0000-0000-000000000003            |
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
	And a funding group version exists in the release management repository
		| Field                        | Value                                           |
		| FundingGroupVersionId        | 00000000-0000-0000-0000-000000000004            |
		| FundingGroupId               | 00000000-0000-0000-0000-000000000001            |
		| Channel                      | Statement                                       |
		| GroupingReason               | Payment                                         |
		| StatusChangedDate            | <CurrentDateTime>                               |
		| MajorVersion                 | 2                                               |
		| MinorVersion                 | 0                                               |
		| TemplateVersion              | 1.2                                             |
		| SchemaVersion                | 1.2                                             |
		| JobId                        | <JobId>                                         |
		| CorrelationId                | <CorrelationId>                                 |
		| FundingStreamId              | 1                                               |
		| FundingPeriodId              | 1                                               |
		| FundingId                    | PSG-AY-2122-Payment-LocalAuthority-10004002-2_0 |
		| TotalFunding                 | 17780                                           |
		| ExternalPublicationDate      | <CurrentDateTime>                               |
		| EarliestPaymentAvailableDate | <CurrentDateTime>                               |
	And a funding group version exists in the release management repository
		| Field                        | Value                                          |
		| FundingGroupVersionId        | 00000000-0000-0000-0000-000000000005           |
		| FundingGroupId               | 00000000-0000-0000-0000-000000000002           |
		| Channel                      | Statement                                      |
		| GroupingReason               | Information                                    |
		| StatusChangedDate            | <CurrentDateTime>                              |
		| MajorVersion                 | 2                                              |
		| MinorVersion                 | 0                                              |
		| TemplateVersion              | 1.2                                            |
		| SchemaVersion                | 1.2                                            |
		| JobId                        | <JobId>                                        |
		| CorrelationId                | <CorrelationId>                                |
		| FundingStreamId              | 1                                              |
		| FundingPeriodId              | 1                                              |
		| FundingId                    | PSG-AY-2122-Information-LocalAuthority-212-2_0 |
		| TotalFunding                 | 17780                                          |
		| ExternalPublicationDate      | <CurrentDateTime>                              |
		| EarliestPaymentAvailableDate | <CurrentDateTime>                              |
	And a funding group version exists in the release management repository
		| Field                        | Value                                           |
		| FundingGroupVersionId        | 00000000-0000-0000-0000-000000000006            |
		| FundingGroupId               | 00000000-0000-0000-0000-000000000003            |
		| Channel                      | Payment                                         |
		| GroupingReason               | Payment                                         |
		| StatusChangedDate            | <CurrentDateTime>                               |
		| MajorVersion                 | 2                                               |
		| MinorVersion                 | 0                                               |
		| TemplateVersion              | 1.2                                             |
		| SchemaVersion                | 1.2                                             |
		| JobId                        | <JobId>                                         |
		| CorrelationId                | <CorrelationId>                                 |
		| FundingStreamId              | 1                                               |
		| FundingPeriodId              | 1                                               |
		| FundingId                    | PSG-AY-2122-Payment-LocalAuthority-10004002-2_0 |
		| TotalFunding                 | 17780                                           |
		| ExternalPublicationDate      | <CurrentDateTime>                               |
		| EarliestPaymentAvailableDate | <CurrentDateTime>                               |
	And the next funding group version identifier for new records should be 7
	And a total of '6' funding group version records exist in the release management repository

	And the provider versions associated with the funding group versions exist in the release management repository
		| FundingGroupProviderId               | FundingGroupVersionId                | ReleasedProviderVersionChannelId     |
		| 00000000-0000-0000-0000-000000000001 | 00000000-0000-0000-0000-000000000001 | 00000000-0000-0000-0000-000000000001 |
		| 00000000-0000-0000-0000-000000000002 | 00000000-0000-0000-0000-000000000002 | 00000000-0000-0000-0000-000000000001 |
		| 00000000-0000-0000-0000-000000000003 | 00000000-0000-0000-0000-000000000003 | 00000000-0000-0000-0000-000000000002 |
		| 00000000-0000-0000-0000-000000000004 | 00000000-0000-0000-0000-000000000004 | 00000000-0000-0000-0000-000000000003 |
		| 00000000-0000-0000-0000-000000000005 | 00000000-0000-0000-0000-000000000005 | 00000000-0000-0000-0000-000000000003 |
		| 00000000-0000-0000-0000-000000000006 | 00000000-0000-0000-0000-000000000006 | 00000000-0000-0000-0000-000000000004 |
	And a total of '6' funding group providers exist in the release management repository
	And the next funding group provider identifier for new records should be 7

	When funding is released to channels for selected providers
		| Field         | Value           |
		| CorrelationId | <CorrelationId> |
		| AuthorName    | <AuthorName>    |
		| AuthorId      | <AuthorId>      |

	Then there is a released provider record in the release management repository
		| Field              | Value                                |
		| ReleasedProviderId | 00000000-0000-0000-0000-000000000001 |
		| SpecificationId    | <SpecificationId>                    |
		| ProviderId         | 10071690                             |
	And there is a total of '1' released provider records in the release management repoistory

	And there is a released provider version record created in the release management repository
		| Field                     | Value                                |
		| ReleasedProviderVersionId | 00000000-0000-0000-0000-000000000003 |
		| ReleasedProviderId        | 00000000-0000-0000-0000-000000000001 |
		| MajorVersion              | 3                                    |
		| FundingId                 | PSG-AY-2122-10071690-3_0             |
		| TotalFunding              | 17780                                |
		| CoreProviderVersionId     | <ProviderVersionId>                  |
	And there is a total of '3' released provider version records in the release management repoistory

	And there is a released provider version channel record created in the release management repository
		| Field                            | Value                                |
		| ReleasedProviderVersionChannelId | 00000000-0000-0000-0000-000000000005 |
		| ReleasedProviderVersionId        | 00000000-0000-0000-0000-000000000003 |
		| Channel                          | Statement                            |
		| StatusChangedDate                | <CurrentDateTime>                    |
		| AuthorId                         | <AuthorId>                           |
		| AuthorName                       | <AuthorName>                         |
	And there is a released provider version channel record created in the release management repository
		| Field                            | Value                                |
		| ReleasedProviderVersionChannelId | 00000000-0000-0000-0000-000000000006 |
		| ReleasedProviderVersionId        | 00000000-0000-0000-0000-000000000003 |
		| Channel                          | Payment                              |
		| StatusChangedDate                | <CurrentDateTime>                    |
		| AuthorId                         | <AuthorId>                           |
		| AuthorName                       | <AuthorName>                         |
	And there are a total of '6' released provider version channel records created in the release management repository

	And there is a released provider channel variation created in the release management repository
		| Field                                    | Value                                |
		| ReleasedProviderChannelVariationReasonId | 00000000-0000-0000-0000-000000000001 |
		| VariationReason                          | FundingUpdated                       |
		| ReleasedProviderVersionChannelId         | 00000000-0000-0000-0000-000000000005 |
	And there is a released provider channel variation created in the release management repository
		| Field                                    | Value                                |
		| ReleasedProviderChannelVariationReasonId | 00000000-0000-0000-0000-000000000002 |
		| VariationReason                          | ProfilingUpdated                     |
		| ReleasedProviderVersionChannelId         | 00000000-0000-0000-0000-000000000005 |
	And there is a released provider channel variation created in the release management repository
		| Field                                    | Value                                |
		| ReleasedProviderChannelVariationReasonId | 00000000-0000-0000-0000-000000000003 |
		| VariationReason                          | CalculationValuesUpdated             |
		| ReleasedProviderVersionChannelId         | 00000000-0000-0000-0000-000000000005 |
	And there is a released provider channel variation created in the release management repository
		| Field                                    | Value                                |
		| ReleasedProviderChannelVariationReasonId | 00000000-0000-0000-0000-000000000004 |
		| VariationReason                          | FundingUpdated                       |
		| ReleasedProviderVersionChannelId         | 00000000-0000-0000-0000-000000000006 |
	And there is a released provider channel variation created in the release management repository
		| Field                                    | Value                                |
		| ReleasedProviderChannelVariationReasonId | 00000000-0000-0000-0000-000000000005 |
		| VariationReason                          | ProfilingUpdated                     |
		| ReleasedProviderVersionChannelId         | 00000000-0000-0000-0000-000000000006 |
	And there are a total of '5' released provider version channel variation reason records created in the release management repository

	And there is a funding group created in the release management repository
		| Field                               | Value                                |
		| FundingGroupId                      | 00000000-0000-0000-0000-000000000001 |
		| SpecificationId                     | <SpecificationId>                    |
		| Channel                             | Statement                            |
		| GroupingReason                      | Payment                              |
		| OrganisationGroupTypeCode           | LocalAuthority                       |
		| OrganisationGroupTypeIdentifier     | UKPRN                                |
		| OrganisationGroupIdentifierValue    | 10004002                             |
		| OrganisationGroupName               | WANDSWORTH LONDON BOROUGH COUNCIL    |
		| OrganisationGroupSearchableName     | WANDSWORTH_LONDON_BOROUGH_COUNCIL    |
		| OrganisationGroupTypeClassification | LegalEntity                          |
	And there is a funding group created in the release management repository
		| Field                               | Value                                |
		| FundingGroupId                      | 00000000-0000-0000-0000-000000000002 |
		| SpecificationId                     | <SpecificationId>                    |
		| Channel                             | Statement                            |
		| GroupingReason                      | Information                          |
		| OrganisationGroupTypeCode           | LocalAuthority                       |
		| OrganisationGroupTypeIdentifier     | LACode                               |
		| OrganisationGroupIdentifierValue    | 212                                  |
		| OrganisationGroupName               | Wandsworth                           |
		| OrganisationGroupSearchableName     | Wandsworth                           |
		| OrganisationGroupTypeClassification | GeographicalBoundary                 |
	And there is a funding group created in the release management repository
		| Field                               | Value                                |
		| FundingGroupId                      | 00000000-0000-0000-0000-000000000003 |
		| SpecificationId                     | <SpecificationId>                    |
		| Channel                             | Payment                              |
		| GroupingReason                      | Payment                              |
		| OrganisationGroupTypeCode           | LocalAuthority                       |
		| OrganisationGroupTypeIdentifier     | UKPRN                                |
		| OrganisationGroupIdentifierValue    | 10004002                             |
		| OrganisationGroupName               | WANDSWORTH LONDON BOROUGH COUNCIL    |
		| OrganisationGroupSearchableName     | WANDSWORTH_LONDON_BOROUGH_COUNCIL    |
		| OrganisationGroupTypeClassification | LegalEntity                          |
	And there are a total of '3' funding group records created in the release management repository

	And there is a funding group version created in the release management repository
		| Field                        | Value                                           |
		| FundingGroupVersionId        | 00000000-0000-0000-0000-000000000007            |
		| FundingGroupId               | 00000000-0000-0000-0000-000000000001            |
		| Channel                      | Statement                                       |
		| GroupingReason               | Payment                                         |
		| StatusChangedDate            | <CurrentDateTime>                               |
		| MajorVersion                 | 3                                               |
		| MinorVersion                 | 0                                               |
		| TemplateVersion              | 1.2                                             |
		| SchemaVersion                | 1.2                                             |
		| JobId                        | <JobId>                                         |
		| CorrelationId                | <CorrelationId>                                 |
		| FundingStreamId              | 1                                               |
		| FundingPeriodId              | 1                                               |
		| FundingId                    | PSG-AY-2122-Payment-LocalAuthority-10004002-3_0 |
		| TotalFunding                 | 17780                                           |
		| ExternalPublicationDate      | <CurrentDateTime>                               |
		| EarliestPaymentAvailableDate | <CurrentDateTime>                               |
	And there is a funding group version created in the release management repository
		| Field                        | Value                                          |
		| FundingGroupVersionId        | 00000000-0000-0000-0000-000000000008           |
		| FundingGroupId               | 00000000-0000-0000-0000-000000000002           |
		| Channel                      | Statement                                      |
		| GroupingReason               | Information                                    |
		| StatusChangedDate            | <CurrentDateTime>                              |
		| MajorVersion                 | 3                                              |
		| MinorVersion                 | 0                                              |
		| TemplateVersion              | 1.2                                            |
		| SchemaVersion                | 1.2                                            |
		| JobId                        | <JobId>                                        |
		| CorrelationId                | <CorrelationId>                                |
		| FundingStreamId              | 1                                              |
		| FundingPeriodId              | 1                                              |
		| FundingId                    | PSG-AY-2122-Information-LocalAuthority-212-3_0 |
		| TotalFunding                 | 17780                                          |
		| ExternalPublicationDate      | <CurrentDateTime>                              |
		| EarliestPaymentAvailableDate | <CurrentDateTime>                              |
	And there is a funding group version created in the release management repository
		| Field                        | Value                                           |
		| FundingGroupVersionId        | 00000000-0000-0000-0000-000000000009            |
		| FundingGroupId               | 00000000-0000-0000-0000-000000000003            |
		| Channel                      | Payment                                         |
		| GroupingReason               | Payment                                         |
		| StatusChangedDate            | <CurrentDateTime>                               |
		| MajorVersion                 | 3                                               |
		| MinorVersion                 | 0                                               |
		| TemplateVersion              | 1.2                                             |
		| SchemaVersion                | 1.2                                             |
		| JobId                        | <JobId>                                         |
		| CorrelationId                | <CorrelationId>                                 |
		| FundingStreamId              | 1                                               |
		| FundingPeriodId              | 1                                               |
		| FundingId                    | PSG-AY-2122-Payment-LocalAuthority-10004002-3_0 |
		| TotalFunding                 | 17780                                           |
		| ExternalPublicationDate      | <CurrentDateTime>                               |
		| EarliestPaymentAvailableDate | <CurrentDateTime>                               |
	And there are a total of '9' funding group version records created in the release management repository

	And there is a funding group variation reason created in the release management repository
		| Field                                | Value                                |
		| FundingGroupVersionVariationReasonId | 00000000-0000-0000-0000-000000000001 |
		| FundingGroupVersionId                | 00000000-0000-0000-0000-000000000007 |
		| VariationReason                      | FundingUpdated                       |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value                                |
		| FundingGroupVersionVariationReasonId | 00000000-0000-0000-0000-000000000002 |
		| FundingGroupVersionId                | 00000000-0000-0000-0000-000000000007 |
		| VariationReason                      | ProfilingUpdated                     |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value                                |
		| FundingGroupVersionVariationReasonId | 00000000-0000-0000-0000-000000000003 |
		| FundingGroupVersionId                | 00000000-0000-0000-0000-000000000008 |
		| VariationReason                      | FundingUpdated                       |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value                                |
		| FundingGroupVersionVariationReasonId | 00000000-0000-0000-0000-000000000004 |
		| FundingGroupVersionId                | 00000000-0000-0000-0000-000000000008 |
		| VariationReason                      | ProfilingUpdated                     |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value                                |
		| FundingGroupVersionVariationReasonId | 00000000-0000-0000-0000-000000000005 |
		| FundingGroupVersionId                | 00000000-0000-0000-0000-000000000009 |
		| VariationReason                      | FundingUpdated                       |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value                                |
		| FundingGroupVersionVariationReasonId | 00000000-0000-0000-0000-000000000006 |
		| FundingGroupVersionId                | 00000000-0000-0000-0000-000000000009 |
		| VariationReason                      | ProfilingUpdated                     |
	And there is a funding group variation reason created in the release management repository
		| Field                                | Value                                |
		| FundingGroupVersionVariationReasonId | 00000000-0000-0000-0000-000000000007 |
		| FundingGroupVersionId                | 00000000-0000-0000-0000-000000000009 |
		| VariationReason                      | CalculationValuesUpdated             |
	And there are a total of '7' funding group version variation reason records created in the release management repository

	And there is the provider version associated with the funding group version in the release management repository
		| Field                            | Value                                |
		| FundingGroupProviderId           | 00000000-0000-0000-0000-000000000007 |
		| FundingGroupVersionId            | 00000000-0000-0000-0000-000000000007 |
		| ReleasedProviderVersionChannelId | 00000000-0000-0000-0000-000000000005 |
	And there is the provider version associated with the funding group version in the release management repository
		| Field                            | Value                                |
		| FundingGroupProviderId           | 00000000-0000-0000-0000-000000000008 |
		| FundingGroupVersionId            | 00000000-0000-0000-0000-000000000008 |
		| ReleasedProviderVersionChannelId | 00000000-0000-0000-0000-000000000005 |
	And there is the provider version associated with the funding group version in the release management repository
		| Field                            | Value                                |
		| FundingGroupProviderId           | 00000000-0000-0000-0000-000000000009 |
		| FundingGroupVersionId            | 00000000-0000-0000-0000-000000000009 |
		| ReleasedProviderVersionChannelId | 00000000-0000-0000-0000-000000000006 |
	And there are a total of '9' funding group providers created in the release management repository

	And there is content blob created for the funding group with ID 'PSG-AY-2122-Information-LocalAuthority-212-3_0' in the channel 'Statement'
	And there is content blob created for the funding group with ID 'PSG-AY-2122-Payment-LocalAuthority-10004002-3_0' in the channel 'Statement'
	And there is content blob created for the funding group with ID 'PSG-AY-2122-Payment-LocalAuthority-10004002-3_0' in the channel 'Payment'
	And there are '3' files contained in the funding groups blob storage

	And there is content blob created for the released published provider with ID 'PSG-AY-2122-10071690-3_0'
	And there are '1' files contained in the published providers blob storage

	And there is content blob created for the released provider with ID 'PSG-AY-2122-10071690-3_0' in channel 'Payment'
	And there is content blob created for the released provider with ID 'PSG-AY-2122-10071690-3_0' in channel 'Statement'
	And there are '2' files contained in the released providers blob storage

Examples:
	| FundingStreamId | FundingPeriodId | SpecificationId                      | Specification Name | ProviderVersionId | ProviderSnapshotId | CurrentDateTime     | AuthorId | AuthorName  | CorrelationId |
	| PSG             | AY-2122         | 3812005f-13b3-4d00-a118-d6cb0e2b2402 | PE and Sport Grant | PSG-2021-10-11-78 | 78                 | 2022-02-10 14:18:00 | AuthId   | Author Name | Corr          |


