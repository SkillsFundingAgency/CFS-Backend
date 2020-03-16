Feature: RefreshPsgVariations
	In order to refresh funding for PE and Sport
	As a funding approver
	I want to refresh funding for all approved providers within a specification
	And for variations with the allocations or provider data to be taken into account 

Background: Existing published funding
    Given a funding configuration exists for funding stream 'PSG' in funding period 'AY-1920'
		| Field                  | Value |
		| DefaultTemplateVersion | 1.0   |
	And variations are enabled
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
	And the funding configuration has the following funding variations
		| Name					| Order |
		| ProviderMetadata		| 0     |
		| NewOpener				| 1     |
		| Closure				| 2     |
		| ClosureWithSuccessor	| 3     |
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
		| ProviderVersionId    | psg-providers-1.0               |
	And the specification has the funding period with id 'AY-1920' and name 'Academic Year 2019-20'
	And the specification has the following funding streams
		| Name          | Id  |
		| PE and Sports | PSG |
	And the specification has the following template versions for funding streams
		| Key | Value |
		| PSG | 1.0   |
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
		| TotalFunding    | 30000    |
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
		| EntityType  | CalculationId | TemplateId | Name				|
		| Calculation | calculation1 | 2		  | Total Allocation	|
		| Calculation | calculation2 | 3		  | Eligible Pupils		|
		| Calculation | calculation3 | 4	      | Pupil rate threshold|
		| Calculation | calculation4 | 5		  | Rate				|
		| Calculation | calculation5 | 6		  | Additional Rate		|
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 2                     | 30000 |
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
		| TotalFunding    | 40000    |
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
		| 2                     | 40000 |
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
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000002                  |
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
		| 2                     | 2600  |
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
		| LACode             | 200                           |
		| LocalAuthorityName | Local Authority 1             |
		| ProviderType       | Academies                     |
		| ProviderSubType    | Academy special sponsor led   |
		| ProviderVersionId  | psg-providers-1.0             |
		| TrustCode          | 1001                          |
		| TrustStatus        | SupportedByAMultiAcademyTrust |
		| UKPRN              | 1000102                       |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
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
	And calculation meta data exists for 'PSG'
	    | CalculationType | CalculationId | Name                 | PublishStatus |
	    | Template        | calculation1  | Total Allocation     | Approved      |
	    | Template        | calculation2  | Eligible Pupils      | Approved      |
	    | Template        | calculation3  | Pupil rate threshold | Approved      |
	    | Template        | calculation4  | Rate                 | Approved      |
	    | Template        | calculation5  | Additional Rate      | Approved      |
	And calculations exists
		| Value         | Id			   |
		| 24000         | calculation1	   |
		| 120			| calculation2	   |
		| 500			| calculation3	   |
		| 1000			| calculation4	   |
		| 20			| calculation5	   |
	And the following distribution periods exist
		| DistributionPeriodId | Value |
		| FY-1920              | 14000 |
		| FY-2021              | 10000 |
	And the following profiles exist
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-1920              | CalendarMonth | October   | 1920 | 1          | 14000         |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 10000         |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 10000         |
		| FY-2021              | CalendarMonth | July      | 2021 | 1          | 10000         |

Scenario: Metadata changed some of the published providers
	Given the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000000                  |
		| Name               | Maintained School 1      |
		| LegalName          | New Legal Name           |
		| CensusWardName     | New Census Ward Name     |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Maintained School 1      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000000                  |
	And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                         |
		| ProviderId         | 1000102                       |
		| Name               | Academy 2                     |
		| CensusWardName     | New Census Ward Name          |
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
	When funding is refreshed
	Then the provider variation reasons were recorded
		| ProviderId | VariationReason            |
		| 1000000    | LegalNameFieldUpdated      |
		| 1000000    | CensusWardNameFieldUpdated |
		| 1000102    | CensusWardNameFieldUpdated |

Scenario: New LACode for a provider after already being published
	Given the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000000                  |
		| Name               | Maintained School 1      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 201                      |
		| LocalAuthorityName | Maintained School 1      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000000                  |
	When funding is refreshed
	Then the provider variation reasons were recorded
		| ProviderId | VariationReason    |
		| 1000000    | LACodeFieldUpdated |

Scenario: New TrustCode for a provider after already being published
	Given the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000000                  |
		| Name               | Maintained School 1      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| TrustCode          | New MAT                  |
		| LocalAuthorityName | Maintained School 1      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | psg-providers-1.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000000                  |
	When funding is refreshed
	Then the provider variation reasons were recorded
		| ProviderId | VariationReason       |
		| 1000000    | TrustCodeFieldUpdated |

Scenario: Provider closes without successor
	Given the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000000                  |
		| Status             | Closed                   |
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
   And the Published Provider '1000000' has the following funding lines
		| Name            | FundingLineCode | Value | TemplateLineId | Type    |
		| TotalAllocation | TotalAllocation | 30000 | 1              | Payment |
   And the Published Provider '1000000' has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-2021              | 30000 |
   And the Published Provider '1000000' distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
		| FY-2021              | CalendarMonth | May       | 2021 | 0          | 700           |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 300           |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 10000         |
   And the following variation pointers exist
		| FundingStreamId | FundingLineId   | PeriodType    | TypeValue | Year | Occurrence |
		| PSG             | TotalAllocation | CalenderMonth | May       | 2021 | 0          |
   When funding is refreshed
   Then the upserted provider version for '1000000' has the following funding line profile periods
		| FundingLineCode | DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| TotalAllocation | FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 0          | 0             |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 1          | 0             |
		| TotalAllocation | FY-2021              | CalendarMonth | June      | 2021 | 1          | 0             |
   And the upserted provider version for '1000000' has the funding line totals 
		| FundingLineCode | Value |
		| TotalAllocation | 10000 |

Scenario: Providers close with successor
	Given the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000000                  |
		| Status             | Closed                   |
		| Successor          | 1000102                  |
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
   And the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000002                  |
		| Status             | Closed                   |
		| Successor          | 1000102                  |
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
   And the Published Provider '1000000' has the following funding lines
		| Name            | FundingLineCode | Value | TemplateLineId | Type    |
		| TotalAllocation | TotalAllocation | 30000 | 1              | Payment |
   And the Published Provider '1000000' has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-2021              | 30000 |
   And the Published Provider '1000000' distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
		| FY-2021              | CalendarMonth | May       | 2021 | 0          | 700           |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 300           |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 10000         |
   And the Published Provider '1000002' has the following funding lines
		| Name            | FundingLineCode | Value | TemplateLineId | Type    |
		| TotalAllocation | TotalAllocation | 40000 | 1              | Payment |
   And the Published Provider '1000002' has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-2021              | 40000 |
   And the Published Provider '1000002' distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 20000         |
		| FY-2021              | CalendarMonth | May       | 2021 | 0          | 700           |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 300           |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 10000         |
   And the Published Provider '1000102' has the following funding lines
		| Name            | FundingLineCode | Value | TemplateLineId | Type    |
		| TotalAllocation | TotalAllocation | 2600  | 1              | Payment |
   And the Published Provider '1000102' has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-2021              | 2600  |
   And the Published Provider '1000102' distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | May       | 2021 | 0          | 600           |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 700           |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 800           |
   And the following variation pointers exist
		| FundingStreamId | FundingLineId   | PeriodType    | TypeValue | Year | Occurrence |
		| PSG             | TotalAllocation | CalenderMonth | May       | 2021 | 0          |
   When funding is refreshed
   Then the upserted provider version for '1000000' has the following funding line profile periods
		| FundingLineCode | DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| TotalAllocation | FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 0          | 0             |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 1          | 0             |
		| TotalAllocation | FY-2021              | CalendarMonth | June      | 2021 | 1          | 0             |
   And the upserted provider version for '1000000' has the funding line totals 
		| FundingLineCode | Value |
		| TotalAllocation | 10000 |
   And the upserted provider version for '1000002' has the following funding line profile periods
		| FundingLineCode | DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| TotalAllocation | FY-2021              | CalendarMonth | April     | 2021 | 1          | 20000         |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 0          | 0             |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 1          | 0             |
		| TotalAllocation | FY-2021              | CalendarMonth | June      | 2021 | 1          | 0             |
   And the upserted provider version for '1000002' has the funding line totals 
		| FundingLineCode | Value |
		| TotalAllocation | 20000 |
   And the upserted provider version for '1000102' has the following funding line profile periods
		| FundingLineCode | DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| TotalAllocation | FY-2021              | CalendarMonth | April     | 2021 | 1          | 500           |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 0          | 2000          |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 1          | 1300          |
		| TotalAllocation | FY-2021              | CalendarMonth | June      | 2021 | 1          | 20800         |
   And the upserted provider version for '1000102' has the funding line totals 
		| FundingLineCode | Value |
		| TotalAllocation | 24600 |
   And the upserted provider version for '1000102' has the following predecessors
		| ProviderId |
		| 1000002    |
		| 1000000    |

Scenario: Providers close with successor but successor not in scope for specification yet
	Given the following provider exists within core provider data in provider version 'psg-providers-1.0'
		| Field              | Value                    |
		| ProviderId         | 1000000                  |
		| Status             | Closed                   |
		| Successor			 | 2000002                  |
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
	And the provider with id '1000000' should be a scoped provider in the current specification in provider version 'psg-providers-1.0'
	And the following provider exists within master provider data
		| Field              | Value                    |
		| ProviderId         | 2000002                  |
		| Status             | Open                     |
		| Name               | Maintained School 2      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2013-04-16               |
		| LACode             | 200                      |
		| LocalAuthorityName | Local Authority 1        |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 2000002                  |
   And the Published Provider '1000000' has the following funding lines
		| Name            | FundingLineCode | Value | TemplateLineId | Type    |
		| TotalAllocation | TotalAllocation | 30000 | 1              | Payment |
   And the Published Provider '1000000' has the following distribution period for funding line 'TotalAllocation'
		| DistributionPeriodId | Value |
		| FY-2021              | 30000 |
   And the Published Provider '1000000' distribution period has the following profiles for funding line 'TotalAllocation'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
		| FY-2021              | CalendarMonth | May       | 2021 | 0          | 700           |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 300           |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 10000         |
   And the following variation pointers exist
		| FundingStreamId | FundingLineId   | PeriodType    | TypeValue | Year | Occurrence |
		| PSG             | TotalAllocation | CalenderMonth | May       | 2021 | 0          |
   When funding is refreshed
   Then the upserted provider version for '1000000' has the following funding line profile periods
		| FundingLineCode | DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| TotalAllocation | FY-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 0          | 0             |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 1          | 0             |
		| TotalAllocation | FY-2021              | CalendarMonth | June      | 2021 | 1          | 0             |
   And the upserted provider version for '1000000' has the funding line totals 
		| FundingLineCode | Value |
		| TotalAllocation | 10000 |
   And the upserted provider version for '2000002' has the following funding line profile periods
		| FundingLineCode | DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| TotalAllocation | FY-2021              | CalendarMonth | April     | 2021 | 1          | 0             |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 0          | 700           |
		| TotalAllocation | FY-2021              | CalendarMonth | May       | 2021 | 1          | 300           |
		| TotalAllocation | FY-2021              | CalendarMonth | June      | 2021 | 1          | 10000         |
   And the upserted provider version for '2000002' has the funding line totals 
		| FundingLineCode | Value |
		| TotalAllocation | 11000 |
   And the upserted provider version for '2000002' has the following predecessors
		| ProviderId |
		| 1000000    |
	