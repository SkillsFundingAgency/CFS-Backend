#this has been hacked about to force change detection in the refresh service code otherwise it fails as the previously Approved providers
#just pick up the same fundling lines etc as before as so don't get upserted during the refresh as nothing changed - have changed some of the
#metadata between the previosuly published and core data to force the upsert - BUT this test setup was very broken and kind of remains so
#I've wasted enough time dealing with this but it should really be replaced to be honest
Feature: RefreshPsg
	In order to refresh funding for PE and Sport
	As a funding approver
	I want to refresh funding for all approved providers within a specification

Background: Existing published funding
	Given a funding configuration exists for funding stream 'PSG' in funding period 'AY-1920'
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
		| Field     | Value                 |
		| Id        | AY-1920               |
		| Name      | Academic Year 2019-20 |
		| StartDate | 2019-08-01 00:00:00   |
		| EndDate   | 2020-07-31 00:00:00   |
		| Period    | 1920                  |
		| Type      | AY                    |
	And the following specification exists
		| Field                | Value                             |
		| Id                   | specForPublishing                 |
		| Name                 | Test Specification for Publishing |
		| IsSelectedForFunding | true                              |
		| ProviderVersionId    | psg-providers-1.0                 |
	And the specification has the funding period with id 'AY-1920' and name 'Academic Year 2019-20'
	And the specification has the following funding streams
		| Name          | Id  |
		| PE and Sports | PSG |
	And the specification has the following template versions for funding streams
		| Key | Value |
		| PSG | 1.0   |
	And the specification is approved
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
		| ProviderVersionId | psg-providers-1.0    |
		| VersionType       | Custom               |
		| Name              | PSG Provider Version |
		| Description       | Acceptance Tests     |
		| Version           | 1                    |
		| TargetDate        | 2019-12-12 00:00     |
		| FundingStream     | PSG                  |
		| Created           | 2019-12-11 00:00     |
	# Maintained schools - Providers
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value    |
		| ProviderId      | 1000000  |
		| FundingStreamId | PSG      |
		| FundingPeriodId | AY-1920  |
		| TemplateVersion | 1.0      |
		| Status          | Approved |
		| TotalFunding    | 12000    |
		| MajorVersion    | 0        |
		| MinorVersion    | 1        |
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
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000000                  |
	And the Published Provider is available in the repository for this specification
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value    |
		| ProviderId      | 1000002  |
		| FundingStreamId | PSG      |
		| FundingPeriodId | AY-1920  |
		| TemplateVersion | 1.0      |
		| Status          | Approved |
		| TotalFunding    | 24000    |
		| MajorVersion    | 0        |
		| MinorVersion    | 1        |
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
		| DateOpened         | 2013-04-17               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000002                  |
	And the Published Provider is available in the repository for this specification
	# PublishedProviders - Academy Trusts
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value    |
		| ProviderId      | 1000101  |
		| FundingStreamId | PSG      |
		| FundingPeriodId | AY-1920  |
		| TemplateVersion | 1.0      |
		| Status          | Approved |
		| TotalFunding    | 24000    |
		| MajorVersion    | 0        |
		| MinorVersion    | 1        |
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
		| DateOpened         | 2013-04-17                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | psg-providers-1.0             |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000101                       |
	And the Published Provider is available in the repository for this specification
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value    |
		| ProviderId      | 1000102  |
		| FundingStreamId | PSG      |
		| FundingPeriodId | AY-1920  |
		| TemplateVersion | 1.0      |
		| Status          | Approved |
		| TotalFunding    | 24000    |
		| MajorVersion    | 0        |
		| MinorVersion    | 1        |
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
		| DateOpened         | 2013-04-17                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | psg-providers-1.0             |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000102                       |
	And the Published Provider is available in the repository for this specification
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value    |
		| ProviderId      | 1000104  |
		| FundingStreamId | PSG      |
		| FundingPeriodId | AY-1920  |
		| TemplateVersion | 1.0      |
		| Status          | Approved |
		| TotalFunding    | 24000    |
		| MajorVersion    | 0        |
		| MinorVersion    | 1        |
	And the Published Provider has the following funding lines
		| Name             | FundingLineCode | Value | TemplateLineId | Type    |
		| Total Allocation | TotalAllocation |       | 1              | Payment |
	And the Published Provider has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-1920              |       |
		| FY-2021              |       |
	And the Published Providers distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          |               |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          |               |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 2                     |       |
		| 3                     |       |
		| 4                     |       |
		| 5                     |       |
		| 6                     |       |
	And the Published Provider has the following provider information
		| Field              | Value                         |
		| ProviderId         | 1000104                       |
		| Name               | Academy 4                     |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-17                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | psg-providers-1.0             |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000104                       |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000000                  |
		| Name               | Maintained School 1      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-16               |
		| LACode             | 200                      |
		| LocalAuthorityName | Maintained School 1      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000000                  |
	And the provider with id '1000000' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000002                  |
		| Name               | Maintained School 2      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000002                  |
	And the provider with id '1000002' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000003                  |
		| Name               | Maintained School 3      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000003                  |
	And the provider with id '1000003' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000004                  |
		| Name               | Maintained School 4      |
		| Authority          | Local Authority 2        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 202                      |
		| LocalAuthorityName | Local Authority 2        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000004                  |
	And the provider with id '1000004' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000005                  |
		| Name               | Maintained School 5      |
		| Authority          | Local Authority 2        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 202                      |
		| LocalAuthorityName | Local Authority 2        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000005                  |
	And the provider with id '1000005' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                                                                       |
		| ProviderId         | 1000009                                                                     |
		| Name               | Maintained School 9  - Excluded for funding, but in scope for specification |
		| Authority          | Local Authority 3                                                           |
		| DateOpened         | 2013-04-16                                                                  |
		| LACode             | 203                                                                         |
		| LocalAuthorityName | Local Authority 3                                                           |
		| ProviderType       | LA maintained schools                                                       |
		| ProviderSubType    | Community school                                                            |
		| ProviderVersionId  | psg-providers-1.0                                                           |
		| TrustStatus        | Not Supported By A Trust                                                    |
		| UKPRN              | 1000009                                                                     |
	And the provider with id '1000009' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	# Academy providers
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                         |
		| ProviderId         | 1000101                       |
		| Name               | Academy 1                     |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | psg-providers-1.0             |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000101                       |
	And the provider with id '1000101' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                         |
		| ProviderId         | 1000102                       |
		| Name               | Academy 2                     |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | psg-providers-1.0             |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000102                       |
	And the provider with id '1000102' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                         |
		| ProviderId         | 1000103                       |
		| Name               | Academy 3                     |
		| Authority          | Local Authority 2             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 2             |
		| ProviderType       | Free Schools                  |
		| ProviderSubType    | Free Schools                  |
		| ProviderVersionId  | psg-providers-1.0             |
		| TrustCode          | 1002                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000103                       |
	And the provider with id '1000103' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                         |
		| ProviderId         | 1000104                       |
		| Name               | Academy 4                     |
		| Authority          | Local Authority 2             |
		| DateOpened         | 2013-04-16                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 2             |
		| ProviderType       | Free Schools                  |
		| ProviderSubType    | Free Schools                  |
		| ProviderVersionId  | psg-providers-1.0             |
		| TrustCode          | 1002                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000104                       |
	And the provider with id '1000104' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	# Local Authorities in Core Provider Data
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 9000000                  |
		| Name               | Local Authority 1        |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | Local Authority          |
		| ProviderSubType    | Local Authority          |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 9000000                  |
		| WardName           |                          |
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 9000002                  |
		| Name               | Local Authority 2        |
		| Authority          | Local Authority 2        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 202                      |
		| LocalAuthorityName | Local Authority 2        |
		| ProviderType       | Local Authority          |
		| ProviderSubType    | Local Authority          |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 9000002                  |
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 9000003                  |
		| Name               | Local Authority 3        |
		| Authority          | Local Authority 3        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 203                      |
		| LocalAuthorityName | Local Authority 3        |
		| ProviderType       | Local Authority          |
		| ProviderSubType    | Local Authority          |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 9000003                  |
	# Academy Trusts
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 8000001                  |
		| Name               | Academy Trust 1          |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 202                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | Academy Trust            |
		| ProviderSubType    | Academy Trust            |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustCode          | 1001                     |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 8000001                  |
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 8000002                  |
		| Name               | Academy Trust 2          |
		| Authority          | Local Authority 2        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 202                      |
		| LocalAuthorityName | Academy Trust 1          |
		| ProviderType       | Academy Trust            |
		| ProviderSubType    | Academy Trust            |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustCode          | 1002                     |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 8000001                  |
	And calculation meta data exists for 'PSG'
		| CalculationType | CalculationId | Name                 | PublishStatus |
		| Template        | calculation1  | Total Allocation     | Approved      |
		| Template        | calculation2  | Eligible Pupils      | Approved      |
		| Template        | calculation3  | Pupil rate threshold | Approved      |
		| Template        | calculation4  | Rate                 | Approved      |
		| Template        | calculation5  | Additional Rate      | Approved      |
	And calculations exists
		| Value | Id           |
		| 24000 | calculation1 |
		| 120   | calculation2 |
		| 500   | calculation3 |
		| 1000  | calculation4 |
		| 20    | calculation5 |
	And the following distribution periods exist
		| DistributionPeriodId | Value |
		| FY-1920              | 14000 |
		| FY-2021              | 10000 |
	And the following profiles exist
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 14000         |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
	And the following profile pattern exists
		|FundingLineId	| FundingStreamId | FundingPeriodId |
		|TotalAllocation| PSG             | AY-1920         |

Scenario: Successful refresh of funding
	When funding is refreshed
	Then the following published provider ids are upserted
		| PublishedProviderId                   | Status  |
		| publishedprovider-1000000-AY-1920-PSG | Updated |
		| publishedprovider-1000002-AY-1920-PSG | Updated |
		| publishedprovider-1000003-AY-1920-PSG | Draft   |
		| publishedprovider-1000004-AY-1920-PSG | Draft   |
		| publishedprovider-1000005-AY-1920-PSG | Draft   |
		| publishedprovider-1000009-AY-1920-PSG | Draft   |
		| publishedprovider-1000101-AY-1920-PSG | Updated |
		| publishedprovider-1000102-AY-1920-PSG | Updated |
		| publishedprovider-1000103-AY-1920-PSG | Draft   |

Scenario: Exclude published providers
	Given calculation meta data exists for 'PSG'
		| CalculationType | CalculationId | Name                 | PublishStatus |
		| Template        | calculation1  | Total Allocation     | Approved      |
		| Template        | calculation2  | Eligible Pupils      | Approved      |
		| Template        | calculation3  | Pupil rate threshold | Approved      |
		| Template        | calculation4  | Rate                 | Approved      |
		| Template        | calculation5  | Additional Rate      | Approved      |
	And calculations exists
		| Value | Id           |
		|       | calculation1 |
		|       | calculation2 |
		|       | calculation3 |
		|       | calculation4 |
		|       | calculation5 |
	When funding is refreshed
	Then the following published provider ids are upserted
		| PublishedProviderId                   | Status  |
		| publishedprovider-1000000-AY-1920-PSG | Updated |
		| publishedprovider-1000002-AY-1920-PSG | Updated |
		| publishedprovider-1000101-AY-1920-PSG | Updated |
		| publishedprovider-1000102-AY-1920-PSG | Updated |

Scenario: No important change made to the providers in core should result in no changes to the published providers
	Given the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 9000000                  |
		| Name               | Local Authority 1        |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| NavVendorNo        | 1234                     |
		| ProviderType       | Local Authority          |
		| ProviderSubType    | Local Authority          |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 9000000                  |
	When funding is refreshed
	Then the following published provider ids are upserted
		| PublishedProviderId                   | Status  |
		| publishedprovider-1000000-AY-1920-PSG | Updated |
		| publishedprovider-1000002-AY-1920-PSG | Updated |
		| publishedprovider-1000003-AY-1920-PSG | Draft   |
		| publishedprovider-1000004-AY-1920-PSG | Draft   |
		| publishedprovider-1000005-AY-1920-PSG | Draft   |
		| publishedprovider-1000009-AY-1920-PSG | Draft   |
		| publishedprovider-1000101-AY-1920-PSG | Updated |
		| publishedprovider-1000102-AY-1920-PSG | Updated |
		| publishedprovider-1000103-AY-1920-PSG | Draft   |

Scenario: Add a new provider to the core provider data and then do a refresh
	Given the provider with id '9000000' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the provider with id '9000002' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the provider with id '9000003' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	When funding is refreshed
	Then the following published provider ids are upserted
		| PublishedProviderId                   | Status  |
		| publishedprovider-1000000-AY-1920-PSG | Updated |
		| publishedprovider-1000002-AY-1920-PSG | Updated |
		| publishedprovider-1000003-AY-1920-PSG | Draft   |
		| publishedprovider-1000004-AY-1920-PSG | Draft   |
		| publishedprovider-1000005-AY-1920-PSG | Draft   |
		| publishedprovider-1000009-AY-1920-PSG | Draft   |
		| publishedprovider-1000101-AY-1920-PSG | Updated |
		| publishedprovider-1000102-AY-1920-PSG | Updated |
		| publishedprovider-1000103-AY-1920-PSG | Draft   |
		| publishedprovider-9000000-AY-1920-PSG | Draft   |
		| publishedprovider-9000002-AY-1920-PSG | Draft   |
		| publishedprovider-9000003-AY-1920-PSG | Draft   |
	And the following funding lines are set against provider with id '1000000'
		| FundingLineCode | Value |
		| TotalAllocation | 12000 |
	And the following funding lines are set against provider with id '9000000'
		| FundingLineCode | Value |
		| TotalAllocation | 24000 |

Scenario: Provider name updated in core provider data and then do a refresh the 'Total Allocation' does not change
	# Given provider name updated in core provider data and then do a refresh the 'Total Allocation' does not change
	Given the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                     |
		| ProviderId         | 1000000                   |
		| Name               | Local Authority Updated 1 |
		| Authority          | Local Authority 1         |
		| DateOpened         | 2012-03-15                |
		| LACode             | 204                       |
		| LocalAuthorityName | Local Authority 1         |
		| ProviderType       | Local Authority           |
		| ProviderSubType    | Local Authority           |
		| ProviderVersionId  | psg-providers-1.0         |
		| TrustStatus        | Not Supported By A Trust  |
		| UKPRN              | 9000000                   |
	When funding is refreshed
	Then the following published provider ids are upserted
		| PublishedProviderId                   | Status  |
		| publishedprovider-1000000-AY-1920-PSG | Updated |
		| publishedprovider-1000002-AY-1920-PSG | Updated |
		| publishedprovider-1000003-AY-1920-PSG | Draft   |
		| publishedprovider-1000004-AY-1920-PSG | Draft   |
		| publishedprovider-1000005-AY-1920-PSG | Draft   |
		| publishedprovider-1000009-AY-1920-PSG | Draft   |
		| publishedprovider-1000101-AY-1920-PSG | Updated |
		| publishedprovider-1000102-AY-1920-PSG | Updated |
		| publishedprovider-1000103-AY-1920-PSG | Draft   |
	And the following funding lines are set against provider with id '1000000'
		| FundingLineCode | Value |
		| TotalAllocation | 12000 |