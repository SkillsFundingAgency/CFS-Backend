﻿Feature: PublishingBatchFundingPsg
	In order to publish funding for PE and Sport
	As a funding approvder
	I want to publish funding for given approved providers within a specification

Scenario Outline: Successful publishing of funding
	Given a funding configuration exists for funding stream '<FundingStreamId>' in funding period '<FundingPeriodId>'
		| Field                  | Value |
		| DefaultTemplateVersion | 1.0   |
	And the funding configuration has the following organisation group
		| Field                     | Value          |
		| GroupTypeIdentifier       | UKPRN          |
		| GroupingReason            | Payment        |
		| GroupTypeClassification   | LegalEntity    |
		| OrganisationGroupTypeCode | LocalAuthority |
	And the funding configuration has the following provider type matches
		| ProviderType          | ProviderSubtype             |
		| LA maintained schools | Community school            |
		| LA maintained schools | Foundation school           |
		| LA maintained schools | Pupil referral unit         |
		| LA maintained schools | Voluntary aided school      |
		| LA maintained schools | Voluntary controlled school |
		| Special schools       | Community special school    |
		| Special schools       | Foundation special school   |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value        |
		| GroupTypeIdentifier       | UKPRN        |
		| GroupingReason            | Payment      |
		| GroupTypeClassification   | LegalEntity  |
		| OrganisationGroupTypeCode | AcademyTrust |
	And the funding configuration has the following provider type matches
		| ProviderType        | ProviderSubtype                           |
		| Free Schools        | Free schools                              |
		| Free Schools        | Free schools alternative provision        |
		| Free Schools        | Free schools special                      |
		| Free Schools        | Free schools 16 to 19                     |
		| Independent schools | City technology college                   |
		| Academies           | Academy alternative provision converter   |
		| Academies           | Academy alternative provision sponsor led |
		| Academies           | Academy converter                         |
		| Academies           | Academy special converter                 |
		| Academies           | Academy special sponsor led               |
		| Academies           | Academy sponsor led                       |
		| Academies           | Academy 16 to 19 sponsor led              |
		| Academies           | Academy 16-19 converter                   |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value       |
		| GroupTypeIdentifier       | UKPRN       |
		| GroupingReason            | Information |
		| GroupTypeClassification   | LegalEntity |
		| OrganisationGroupTypeCode | Provider    |
	And the funding configuration has the following provider type matches
		| ProviderType    | ProviderSubtype               |
		| Special schools | Non-maintained special school |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value                |
		| GroupTypeIdentifier       | LACode               |
		| GroupingReason            | Information          |
		| GroupTypeClassification   | GeographicalBoundary |
		| OrganisationGroupTypeCode | LocalAuthority       |
	And the funding configuration is available in the policies repository
	And the funding period exists in the policies service
		| Field     | Value               |
		| Id        | <FundingPeriodId>   |
		| Name      | <FundingPeriodName> |
		| StartDate | 2019-08-01 00:00:00 |
		| EndDate   | 2020-07-31 00:00:00 |
		| Period    | 1920                |
		| Type      | AY                  |
	And the following specification exists
		| Field                | Value                             |
		| Id                   | specForPublishing                 |
		| Name                 | Test Specification for Publishing |
		| IsSelectedForFunding | true                              |
		| ProviderVersionId    | <ProviderVersionId>               |
	And the specification has the funding period with id '<FundingPeriodId>' and name '<FundingPeriodName>'
	And the specification has the following funding streams
		| Name          | Id                |
		| PE and Sports | <FundingStreamId> |
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
	And the following provider version exists in the providers service
		| Field             | Value                |
		| ProviderVersionId | <ProviderVersionId>  |
		| VersionType       | Custom               |
		| Name              | PSG Provider Version |
		| Description       | Acceptance Tests     |
		| Version           | 1                    |
		| TargetDate        | 2019-12-12 00:00     |
		| FundingStream     | <FundingStreamId>    |
		| Created           | 2019-12-11 00:00     |
	# Maintained schools - PublishedProviders
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000000           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 12000             |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name             | FundingLineCode | Value | TemplateLineId | Type    |
		| Total Allocation | TotalAllocation | 12000 | 1              | Payment |
	And the Published Provider has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-1920              | 7000  |
		| FY-2021              | 5000  |
	And the Published Providers distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 7000          |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 5000          |
	And template mapping exists
		| EntityType  | CalculationId | TemplateId | Name                 |
		| Calculation | calculation1  | 2          | Total Allocation     |
		| Calculation | calculation2  | 3          | Eligible Pupils      |
		| Calculation | calculation3  | 4          | Pupil rate threshold |
		| Calculation | calculation4  | 5          | Rate                 |
		| Calculation | calculation5  | 6          | Additional Rate      |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 2                     | 12000 |
		| 3                     | 120   |
		| 4                     | 500   |
		| 5                     | 1000  |
		| 6                     | 20    |
	And the Published Provider has the following provider information
		| Field              | Value                    |
		| ProviderId         | 1000000                  |
		| Name               | Maintained School 1      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Maintained School 1      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000000                  |
	And the Published Provider is available in the repository for this specification
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000002           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 24000             |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name             | FundingLineCode | Value | TemplateLineId | Type    |
		| Total Allocation | TotalAllocation | 24000 | 1              | Payment |
	And the Published Provider has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-1920              | 14000 |
		| FY-2021              | 10000 |
	And the Published Providers distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 14000         |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 2                     | 24000 |
		| 3                     | 120   |
		| 4                     | 500   |
		| 5                     | 1000  |
		| 6                     | 20    |
	And the Published Provider has the following provider information
		| Field              | Value                    |
		| ProviderId         | 1000002                  |
		| Name               | Maintained School 2      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000002                  |
	And the Published Provider is available in the repository for this specification
	# PublishedProviders - Academy Trusts
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000101           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 24000             |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name             | FundingLineCode | Value | TemplateLineId | Type    |
		| Total Allocation | TotalAllocation | 24000 | 1              | Payment |
	And the Published Provider has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-1920              | 14000 |
		| FY-2021              | 10000 |
	And the Published Providers distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 14000         |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 2                     | 24000 |
		| 3                     | 120   |
		| 4                     | 500   |
		| 5                     | 1000  |
		| 6                     | 20    |
	And the Published Provider has the following provider information
		| Field              | Value                         |
		| ProviderId         | 1000101                       |
		| Name               | Academy 1                     |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | <ProviderVersionId>           |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000101                       |
	And the Published Provider is available in the repository for this specification
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000102           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 24000             |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name             | FundingLineCode | Value | TemplateLineId | Type    |
		| Total Allocation | TotalAllocation | 24000 | 1              | Payment |
	And the Published Provider has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-1920              | 14000 |
		| FY-2021              | 10000 |
	And the Published Providers distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 14000         |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 2                     | 24000 |
		| 3                     | 120   |
		| 4                     | 500   |
		| 5                     | 1000  |
		| 6                     | 20    |
	And the Published Provider has the following provider information
		| Field              | Value                         |
		| ProviderId         | 1000102                       |
		| Name               | Academy 2                     |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 400                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | <ProviderVersionId>           |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000102                       |
	And the Published Provider is available in the repository for this specification
	# PublishedProviders - Providers - Non-maintained special school
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000201           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 44000             |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
		| Version         | 1                 |
	And the Published Provider has the following funding lines
		| Name             | FundingLineCode | Value | TemplateLineId | Type    |
		| Total Allocation | TotalAllocation | 44000 | 1              | Payment |
	And the Published Provider has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-1920              | 24000 |
		| FY-2021              | 20000 |
	And the Published Providers distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 24000         |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 20000         |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 2                     | 24000 |
		| 3                     | 120   |
		| 4                     | 500   |
		| 5                     | 1000  |
		| 6                     | 20    |
	And the Published Provider has the following provider information
		| Field              | Value                         |
		| ProviderId         | 1000201                       |
		| Name               | Non-Maintained School 1       |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Special schools               |
		| ProviderSubType    | Non-maintained special school |
		| ProviderVersionId  | <ProviderVersionId>           |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000201                       |
	And the Published Provider is available in the repository for this specification
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000202           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 44000             |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name             | FundingLineCode | Value | TemplateLineId | Type    |
		| Total Allocation | TotalAllocation | 44000 | 1              | Payment |
	And the Published Provider has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-1920              | 24000 |
		| FY-2021              | 20000 |
	And the Published Providers distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 24000         |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 20000         |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 2                     | 24000 |
		| 3                     | 120   |
		| 4                     | 500   |
		| 5                     | 1000  |
		| 6                     | 20    |
	And the Published Provider has the following provider information
		| Field              | Value                         |
		| ProviderId         | 1000202                       |
		| Name               | Non-Maintained School 1       |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Special schools               |
		| ProviderSubType    | Non-maintained special school |
		| ProviderVersionId  | <ProviderVersionId>           |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000202                       |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 1000000                  |
		| Name               | Maintained School 1      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Maintained School 1      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000000                  |
	And the provider with id '1000000' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 1000002                  |
		| Name               | Maintained School 2      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000002                  |
	And the provider with id '1000002' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 1000003                  |
		| Name               | Maintained School 3      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000003                  |
	And the provider with id '1000003' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 1000004                  |
		| Name               | Maintained School 4      |
		| Authority          | Local Authority 2        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 202                      |
		| LocalAuthorityName | Local Authority 2        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000004                  |
	And the provider with id '1000004' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 1000005                  |
		| Name               | Maintained School 5      |
		| Authority          | Local Authority 2        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 202                      |
		| LocalAuthorityName | Local Authority 2        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000005                  |
	And the provider with id '1000005' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                                                                       |
		| ProviderId         | 1000009                                                                     |
		| Name               | Maintained School 9  - Excluded for funding, but in scope for specification |
		| Authority          | Local Authority 3                                                           |
		| DateOpened         | 2013-04-16                                                                  |
		| LACode             | 203                                                                         |
		| LocalAuthorityName | Local Authority 3                                                           |
		| ProviderType       | LA maintained schools                                                       |
		| ProviderSubType    | Community school                                                            |
		| ProviderVersionId  | <ProviderVersionId>                                                         |
		| TrustStatus        | Not Supported By A Trust                                                    |
		| UKPRN              | 1000009                                                                     |
	And the provider with id '1000009' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	# Academy providers
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                         |
		| ProviderId         | 1000101                       |
		| Name               | Academy 1                     |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | <ProviderVersionId>           |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000101                       |
	And the provider with id '1000101' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                         |
		| ProviderId         | 1000102                       |
		| Name               | Academy 2                     |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | <ProviderVersionId>           |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000102                       |
	And the provider with id '1000102' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                         |
		| ProviderId         | 1000103                       |
		| Name               | Academy 3                     |
		| Authority          | Local Authority 2             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 2             |
		| ProviderType       | Free Schools                  |
		| ProviderSubType    | Free Schools                  |
		| ProviderVersionId  | <ProviderVersionId>           |
		| TrustCode          | 1002                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000103                       |
	And the provider with id '1000103' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	# Non-Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                         |
		| Field              | Value                         |
		| ProviderId         | 1000201                       |
		| Name               | Non-Maintained School 1       |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Special schools               |
		| ProviderSubType    | Non-maintained special school |
		| ProviderVersionId  | <ProviderVersionId>           |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000201                       |
	And the provider with id '1000201' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                         |
		| Field              | Value                         |
		| ProviderId         | 1000202                       |
		| Name               | Non-Maintained School 1       |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Special schools               |
		| ProviderSubType    | Non-maintained special school |
		| ProviderVersionId  | <ProviderVersionId>           |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000202                       |
	And the provider with id '1000202' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	# Local Authorities in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 9000000                  |
		| Name               | Local Authority 1        |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | Local Authority          |
		| ProviderSubType    | Local Authority          |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 9000000                  |
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 9000002                  |
		| Name               | Local Authority 2        |
		| Authority          | Local Authority 2        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 202                      |
		| LocalAuthorityName | Local Authority 2        |
		| ProviderType       | Local Authority          |
		| ProviderSubType    | Local Authority          |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 9000002                  |
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 9000003                  |
		| Name               | Local Authority 3        |
		| Authority          | Local Authority 3        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 202                      |
		| LocalAuthorityName | Local Authority 3        |
		| ProviderType       | Local Authority          |
		| ProviderSubType    | Local Authority          |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 9000003                  |
	# Academy Trusts
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 8000001                  |
		| Name               | Academy Trust 1          |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 202                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | Academy trust            |
		| ProviderSubType    | Academy trust            |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustCode          | 1001                     |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 8000001                  |
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 8000002                  |
		| Name               | Academy Trust 2          |
		| Authority          | Local Authority 2        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 202                      |
		| LocalAuthorityName | Academy Trust 1          |
		| ProviderType       | Academy Trust            |
		| ProviderSubType    | Academy Trust            |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustCode          | 1002                     |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 8000001                  |
	And calculations exists
		| Value | Id           |
		| 24000 | calculation1 |
		| 120   | calculation2 |
		| 500   | calculation3 |
		| 1000  | calculation4 |
		| 20    | calculation5 |
	When batch funding is published
		| Ids     |
		| <FundingStreamId>-<FundingPeriodId>-1000000 |
		| <FundingStreamId>-<FundingPeriodId>-1000002 |
		| <FundingStreamId>-<FundingPeriodId>-1000101 |
	Then the following published funding is produced
		| Field                            | Value             |
		| GroupingReason                   | Payment           |
		| OrganisationGroupTypeCode        | LocalAuthority    |
		| OrganisationGroupIdentifierValue | 9000000           |
		| FundingPeriodId                  | <FundingPeriodId> |
		| FundingStreamId                  | <FundingStreamId> |
	And the total funding is '36000'
	And the published funding contains the following published provider ids
		| FundingIds                                      |
		| <FundingStreamId>-<FundingPeriodId>-1000000-1_0 |
		| <FundingStreamId>-<FundingPeriodId>-1000002-1_0 |
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-1920' has the value of '21000'
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-2021' has the value of '15000'
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-1920' has the following profiles
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 21000         |
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-2021' has the following profiles
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 15000         |
	And the following published funding is produced
		| Field                            | Value             |
		| GroupingReason                   | Payment           |
		| OrganisationGroupTypeCode        | AcademyTrust      |
		| OrganisationGroupIdentifierValue | 8000001           |
		| FundingPeriodId                  | <FundingPeriodId> |
		| FundingStreamId                  | <FundingStreamId> |
	And the total funding is '48000'
	And the published funding contains the following published provider ids
		| FundingIds                                      |
		| <FundingStreamId>-<FundingPeriodId>-1000101-1_0 |
		| <FundingStreamId>-<FundingPeriodId>-1000102-0_1 |
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-1920' has the value of '28000'
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-2021' has the value of '20000'
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-1920' has the following profiles
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 28000         |
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-2021' has the following profiles
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 20000         |
	And  the published funding contains a calculations in published provider with following calculation results
		| Id | Value |
		| 3  | 240   |
		| 5  | 1000  |
		| 6  | 20    |
	And the published funding document produced is saved to blob storage for following file name
		| PublishedFundingFiles                                                       |
		| <FundingStreamId>-<FundingPeriodId>-Information-Provider-1000201-1_0.json   |
		| <FundingStreamId>-<FundingPeriodId>-Information-Provider-1000202-1_0.json   |
		| <FundingStreamId>-<FundingPeriodId>-Payment-AcademyTrust-8000001-1_0.json   |
		| <FundingStreamId>-<FundingPeriodId>-Payment-LocalAuthority-9000000-1_0.json |
	And the published funding document produced has following metadata
		| PublishedFundingFiles                                                       | MetadataKey      | MetadataValue     |
		| <FundingStreamId>-<FundingPeriodId>-Information-Provider-1000201-1_0.json   | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-Information-Provider-1000202-1_0.json   | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-Payment-AcademyTrust-8000001-1_0.json   | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-Payment-LocalAuthority-9000000-1_0.json | specification-id | specForPublishing |
	And the published provider document produced is saved to blob storage for following file name
		| PublishedProviderFiles                               |
		| <FundingStreamId>-<FundingPeriodId>-1000000-1_0.json |
		| <FundingStreamId>-<FundingPeriodId>-1000002-1_0.json |
		| <FundingStreamId>-<FundingPeriodId>-1000101-1_0.json |
		| <FundingStreamId>-<FundingPeriodId>-1000102-1_0.json |
		| <FundingStreamId>-<FundingPeriodId>-1000201-1_0.json |
		| <FundingStreamId>-<FundingPeriodId>-1000202-1_0.json |
	And the published provider document produced has following metadata
		| PublishedFundingFiles                                | MetadataKey      | MetadataValue     |
		| <FundingStreamId>-<FundingPeriodId>-1000000-1_0.json | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-1000002-1_0.json | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-1000101-1_0.json | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-1000102-1_0.json | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-1000201-1_0.json | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-1000202-1_0.json | specification-id | specForPublishing |
	And the following published provider search index items is produced for providerid with '<FundingStreamId>' and '<FundingPeriodId>'
		| ID                  | ProviderType          | ProviderSubType             | LocalAuthority    | FundingStatus | ProviderName        | UKPRN   | FundingValue | SpecificationId   | FundingStreamId   | FundingPeriodId   | Errors |
		| PSG-AY-1920-1000101 | Academies             | Academy special sponsor led | Local Authority 1 | Released      | Academy 1           | 1000101 | 24000        | specForPublishing | <FundingStreamId> | <FundingPeriodId> | |
		| PSG-AY-1920-1000002 | LA maintained schools | Community school            | Local Authority 1 | Released      | Maintained School 2 | 1000002 | 24000        | specForPublishing | <FundingStreamId> | <FundingPeriodId> | |
	And the following job is requested is completed for the current specification
		| Field                  | Value             |
		| JobDefinitionId        | PublishFundingJob |
		| InvokerUserId          | PublishUserId     |
		| InvokerUserDisplayName | Invoker User      |
		| ParentJobId            |                   |
	And the following released published provider ids are upserted
		| PublishedProviderId                                           | Status   |
		| publishedprovider-1000000-<FundingPeriodId>-<FundingStreamId> | Released |
		| publishedprovider-1000002-<FundingPeriodId>-<FundingStreamId> | Released |
		| publishedprovider-1000101-<FundingPeriodId>-<FundingStreamId> | Released |
		| publishedprovider-1000102-<FundingPeriodId>-<FundingStreamId> | Approved |
		| publishedprovider-1000201-<FundingPeriodId>-<FundingStreamId> | Approved |
		| publishedprovider-1000202-<FundingPeriodId>-<FundingStreamId> | Approved |

	Examples:
		| FundingStreamId | FundingPeriodId | FundingPeriodName     | TemplateVersion | ProviderVersionId |
		| PSG             | AY-1920         | Academic Year 2019-20 | 1.0             | psg-providers-1.0 |