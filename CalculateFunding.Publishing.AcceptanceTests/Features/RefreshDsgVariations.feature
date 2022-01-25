Feature: RefreshDsgVariations
	In order to refresh funding for DSG
	As a funding approver
	I want to refresh funding for all approved providers within a specification
	And for variations with the allocations to be taken into account 

Background:
	Given a funding configuration exists for funding stream 'DSG' in funding period 'FY-2021'
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
		| Field     | Value                  |
		| Id        | FY-2021                |
		| Name      | Financial Year 2020-21 |
		| StartDate | 2019-08-01 00:00:00    |
		| EndDate   | 2020-07-31 00:00:00    |
		| Period    | 2021                   |
		| Type      | FY                     |
	And the following specification exists
		| Field                | Value                             |
		| Id                   | specForPublishing                 |
		| Name                 | Test Specification for Publishing |
		| IsSelectedForFunding | true                              |
		| ProviderVersionId    | dsg-providers-2.0                 |
	And the specification has the funding period with id 'FY-2021' and name 'Financial Year 2020-21'
	And the specification has the following funding streams
		| Name | Id  |
		| DSG  | DSG |
	And the specification has the following template versions for funding streams
		| Key | Value |
		| DSG | 1.0.Variations   |
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
		| ProviderVersionId | dsg-providers-2.0    |
		| VersionType       | Custom               |
		| Name              | DSG Provider Version |
		| Description       | Acceptance Tests     |
		| Version           | 2                    |
		| TargetDate        | 2019-12-12 00:00     |
		| FundingStream     | DSG                  |
		| Created           | 2019-12-11 00:00     |
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value    |
		| ProviderId      | 1000000  |
		| FundingStreamId | DSG      |
		| FundingPeriodId | FY-2021  |
		| TemplateVersion | 1.0      |
		| Status          | Released |
		| TotalFunding    | 14000    |
		| MajorVersion    | 0        |
		| MinorVersion    | 1        |
	And the Published Provider has the following funding lines
		| Name                                      | FundingLineCode | Value | TemplateLineId | Type    |
		| Total DSG after deductions and recoupment | DSG-002         | 14000 | 3              | Payment |
	And the Published Provider has the following distribution period for funding line 'DSG-002'
		| DistributionPeriodId | Value |
		| FY-2021              | 14000 |
	And the Published Providers distribution period has the following profiles for funding line 'DSG-002'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 2000          |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 2000          |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 2000          |
		| FY-2021              | CalendarMonth | July      | 2021 | 1          | 2000          |
		| FY-2021              | CalendarMonth | August    | 2021 | 1          | 2000          |
		| FY-2021              | CalendarMonth | September | 2021 | 1          | 2000          |
		| FY-2021              | CalendarMonth | October   | 2021 | 1          | 2000          |
	And template mapping exists
		| EntityType  | CalculationId                        | TemplateId | Name                   |
		| Calculation | 5cfb28de-88d6-4faa-a936-d81a065fb596 | 219        | Stub for total funding | 
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
		| ProviderVersionId  | dsg-providers-2.0        |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000000                  |
	And the Published Provider is available in the repository for this specification
	And the following provider exists within core provider data in provider version 'dsg-providers-2.0'
		| Field              | Value                         |
		| ProviderId         | 1000000                       |
		| Name               | Maintained School 1 - Changed |
		| Authority          | Local Authority 1             |
		| DateOpened         | 2012-03-15                    |
		| LACode             | 200                           |
		| LocalAuthorityName | Maintained School 1           |
		| ProviderType       | LA maintained schools         |
		| ProviderSubType    | Community school              |
		| ProviderVersionId  | dsg-providers-2.0             |
		| TrustStatus        | Not Supported By A Trust      |
		| UKPRN              | 1000000                       |
	And the provider with id '1000000' should be a scoped provider in the current specification in provider version 'dsg-providers-2.0'
	# Local Authorities in Core Provider Data
	And the following provider exists within core provider data in provider version 'dsg-providers-2.0'
		| Field                         | Value                    |
		| ProviderId                    | 9000000                  |
		| Name                          | Local Authority 1        |
		| Authority                     | Local Authority 1        |
		| DateOpened                    | 2012-03-15               |
		| LACode                        | 200                      |
		| LocalAuthorityName            | Local Authority 1        |
		| ProviderType                  | Local Authority          |
		| ProviderSubType               | Local Authority          |
		| ProviderVersionId             | <ProviderVersionId>      |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 9000000                  |
	And calculation meta data exists for 'DSG'
	    | CalculationType | CalculationId                        | Name                   | PublishStatus |
	    | Template        | 5cfb28de-88d6-4faa-a936-d81a065fb596 | Stub for total funding | Approved      |
	And variations are enabled
	And the funding configuration has the following funding variations
		| Name                     | Order |
		| ProviderMetadata         | 0     |
		| DsgTotalAllocationChange | 1     |
		| FundingUpdated           | 2     |
		| ProfilingUpdated         | 3     |
		| PupilNumberSuccessor     | 4     |
	And the following profile pattern exists
		| FundingLineId	|FundingStreamId | FundingPeriodId |
		| DSG-002		|DSG | FY-2021 |

Scenario: When the total allocation increases
    Given the following variation pointers exist
		| FundingStreamId | FundingLineId | PeriodType    | TypeValue | Year | Occurrence |
		| DSG             | DSG-002       | CalenderMonth | June      | 2021 | 1          |
	And the following calculation results also exist
		| Value | Id                                   |
		| 21000 | 5cfb28de-88d6-4faa-a936-d81a065fb596 |
	And the following distribution periods exist
		| DistributionPeriodId | Value |
		| FY-2021              | 21000 |
	And the following profiles exist
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 3000          |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 3000          |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 3000          |
		| FY-2021              | CalendarMonth | July      | 2021 | 1          | 3000          |
		| FY-2021              | CalendarMonth | August    | 2021 | 1          | 3000          |
		| FY-2021              | CalendarMonth | September | 2021 | 1          | 3000          |
		| FY-2021              | CalendarMonth | October   | 2021 | 1          | 3000          |
	When funding is refreshed
	Then the upserted provider version for '1000000' has the following funding line profile periods
		| FundingLineCode | DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| DSG-002         | FY-2021              | CalendarMonth | April     | 2021 | 1          | 2000          |
		| DSG-002         | FY-2021              | CalendarMonth | May       | 2021 | 1          | 2000          |
		| DSG-002         | FY-2021              | CalendarMonth | June      | 2021 | 1          | 5000          |
		| DSG-002         | FY-2021              | CalendarMonth | July      | 2021 | 1          | 3000          |
		| DSG-002         | FY-2021              | CalendarMonth | August    | 2021 | 1          | 3000          |
		| DSG-002         | FY-2021              | CalendarMonth | September | 2021 | 1          | 3000          |
		| DSG-002         | FY-2021              | CalendarMonth | October   | 2021 | 1          | 3000          |
	And the upserted provider version for '1000000' has the funding line totals
	    | FundingLineCode | Value |
	    | TotalAllocation | 21000 |
	And the upserted provider version for '1000000' has no funding line over payments for funding line 'DSG-002'
	And the provider variation reasons were recorded
		| ProviderId | VariationReason  |
		| 1000000    | FundingUpdated   |
		| 1000000    | NameFieldUpdated |
		| 1000000    | ProfilingUpdated |

Scenario: When the total allocation decreases but repaid in period
    Given the following variation pointers exist
		| FundingStreamId | FundingLineId | PeriodType    | TypeValue | Year | Occurrence |
		| DSG             | DSG-002       | CalenderMonth | May       | 2021 | 1          |
	And the following calculation results also exist
		| Value | Id                                   |
		| 3500  | 5cfb28de-88d6-4faa-a936-d81a065fb596 |
	And the following distribution periods exist
		| DistributionPeriodId | Value |
		| FY-2021              | 3500 |
	And the following profiles exist
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | July      | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | August    | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | September | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | October   | 2021 | 1          | 500           |
	When funding is refreshed
	Then the upserted provider version for '1000000' has the following funding line profile periods
		| FundingLineCode | DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| DSG-002         | FY-2021              | CalendarMonth | April     | 2021 | 1          | 2000          |
		| DSG-002         | FY-2021              | CalendarMonth | May       | 2021 | 1          | 0             |
		| DSG-002         | FY-2021              | CalendarMonth | June      | 2021 | 1          | 0             |
		| DSG-002         | FY-2021              | CalendarMonth | July      | 2021 | 1          | 0             |
		| DSG-002         | FY-2021              | CalendarMonth | August    | 2021 | 1          | 500           |
		| DSG-002         | FY-2021              | CalendarMonth | September | 2021 | 1          | 500           |
		| DSG-002         | FY-2021              | CalendarMonth | October   | 2021 | 1          | 500           |
	And the upserted provider version for '1000000' has the funding line totals
	    | FundingLineCode | Value |
	    | TotalAllocation | 3500  |
	And the upserted provider version for '1000000' has no funding line over payments for funding line 'DSG-002'
	And the provider variation reasons were recorded
		| ProviderId | VariationReason  |
		| 1000000    | FundingUpdated   |
		| 1000000    | NameFieldUpdated |
		| 1000000    | ProfilingUpdated |

Scenario: When the total allocation decreases and leaves an overpayment outside of the period
    Given the following variation pointers exist
		| FundingStreamId | FundingLineId | PeriodType    | TypeValue | Year | Occurrence |
		| DSG             | DSG-002       | CalenderMonth | September | 2021 | 1          |
	And the following calculation results also exist
		| Value | Id                                   |
		| 3500  | 5cfb28de-88d6-4faa-a936-d81a065fb596 |
	And the following distribution periods exist
		| DistributionPeriodId | Value |
		| FY-2021              | 3500 |
	And the following profiles exist
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| FY-2021              | CalendarMonth | April     | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | May       | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | June      | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | July      | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | August    | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | September | 2021 | 1          | 500           |
		| FY-2021              | CalendarMonth | October   | 2021 | 1          | 500           |
	When funding is refreshed
	Then the upserted provider version for '1000000' has the following funding line profile periods
		| FundingLineCode | DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| DSG-002         | FY-2021              | CalendarMonth | April     | 2021 | 1          | 2000          |
		| DSG-002         | FY-2021              | CalendarMonth | May       | 2021 | 1          | 2000          |
		| DSG-002         | FY-2021              | CalendarMonth | June      | 2021 | 1          | 2000          |
		| DSG-002         | FY-2021              | CalendarMonth | July      | 2021 | 1          | 2000          |
		| DSG-002         | FY-2021              | CalendarMonth | August    | 2021 | 1          | 2000          |
		| DSG-002         | FY-2021              | CalendarMonth | September | 2021 | 1          | 0             |
		| DSG-002         | FY-2021              | CalendarMonth | October   | 2021 | 1          | 0             |
	And the upserted provider version for '1000000' has the funding line totals
	    | FundingLineCode | Value  |
		| TotalAllocation | 10000  |
	And the upserted provider version for '1000000' has the following funding line over payments
	    | FundingLineCode | OverPayment |
	    | DSG-002         | 6500        |
	And the provider variation reasons were recorded
		| ProviderId | VariationReason  |
		| 1000000    | FundingUpdated   |
		| 1000000    | NameFieldUpdated |
		| 1000000    | ProfilingUpdated |
