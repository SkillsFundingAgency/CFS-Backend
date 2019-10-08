Feature: PublishingPsg
	In order to publish funding for PE and Sport
	As a funding approvder
	I want to publish funding for all approved providers within a specification

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
		| MajorVersion    | 1                 |
		| MinorVersion    | 0                 |
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
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 2                     | 12000 |
		| 3                     | 120   |
		| 4                     | 500   |
		| 5                     | 1000  |
		| 6                     | 20    |
	And the Published Provider has the following provider information
		| Field                         | Value                    |
		| ProviderId                    | 1000000                  |
		| Name                          | Maintained School 1      |
		| Authority                     | Local Authority 1        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2012-03-15               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 200                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Maintained School 1      |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000000                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	And the Published Provider is available in the repository for this specification
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000002           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 24000             |
		| MajorVersion    | 1                 |
		| MinorVersion    | 0                 |
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
		| Field                         | Value                    |
		| ProviderId                    | 1000002                  |
		| Name                          | Maintained School 2      |
		| Authority                     | Local Authority 1        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2013-04-16               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 200                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Local Authority 1        |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000002                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
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
		| MajorVersion    | 1                 |
		| MinorVersion    | 0                 |
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
		| Field                         | Value                         |
		| ProviderId                    | 1000101                       |
		| Name                          | Academy 1                     |
		| Authority                     | Local Authority 1             |
		| CensusWardCode                |                               |
		| CensusWardName                |                               |
		| CompaniesHouseNumber          |                               |
		| CountryCode                   |                               |
		| CountryName                   |                               |
		| CrmAccountId                  |                               |
		| DateClosed                    |                               |
		| DateOpened                    | 2013-04-16                    |
		| DfeEstablishmentNumber        |                               |
		| DistrictCode                  |                               |
		| DistrictName                  |                               |
		| EstablishmentNumber           |                               |
		| GovernmentOfficeRegionCode    |                               |
		| GovernmentOfficeRegionName    |                               |
		| GroupIdNumber                 |                               |
		| LACode                        | 200                           |
		| LegalName                     |                               |
		| LocalAuthorityName            | Local Authority 1             |
		| LowerSuperOutputAreaCode      |                               |
		| LowerSuperOutputAreaName      |                               |
		| MiddleSuperOutputAreaCode     |                               |
		| MiddleSuperOutputAreaName     |                               |
		| NavVendorNo                   |                               |
		| ParliamentaryConstituencyCode |                               |
		| ParliamentaryConstituencyName |                               |
		| PhaseOfEducation              |                               |
		| Postcode                      |                               |
		| ProviderProfileIdType         |                               |
		| ProviderType                  | Academies                     |
		| ProviderSubType               | Academy special sponsor led   |
		| ProviderVersionId             | <ProviderVersionId>           |
		| ReasonEstablishmentClosed     |                               |
		| ReasonEstablishmentOpened     |                               |
		| RscRegionCode                 |                               |
		| RscRegionName                 |                               |
		| Status                        |                               |
		| Successor                     |                               |
		| Town                          |                               |
		| TrustCode                     | 1001                          |
		| TrustName                     |                               |
		| TrustStatus                   | SupportedByAMultiAcademyTrust |
		| UKPRN                         | 1000101                       |
		| UPIN                          |                               |
		| URN                           |                               |
		| WardCode                      |                               |
		| WardName                      |                               |
	And the Published Provider is available in the repository for this specification
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000102           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 24000             |
		| MajorVersion    | 1                 |
		| MinorVersion    | 0                 |
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
		| Field                         | Value                         |
		| ProviderId                    | 1000102                       |
		| Name                          | Academy 2                     |
		| Authority                     | Local Authority 1             |
		| CensusWardCode                |                               |
		| CensusWardName                |                               |
		| CompaniesHouseNumber          |                               |
		| CountryCode                   |                               |
		| CountryName                   |                               |
		| CrmAccountId                  |                               |
		| DateClosed                    |                               |
		| DateOpened                    | 2013-04-16                    |
		| DfeEstablishmentNumber        |                               |
		| DistrictCode                  |                               |
		| DistrictName                  |                               |
		| EstablishmentNumber           |                               |
		| GovernmentOfficeRegionCode    |                               |
		| GovernmentOfficeRegionName    |                               |
		| GroupIdNumber                 |                               |
		| LACode                        | 200                           |
		| LegalName                     |                               |
		| LocalAuthorityName            | Local Authority 1             |
		| LowerSuperOutputAreaCode      |                               |
		| LowerSuperOutputAreaName      |                               |
		| MiddleSuperOutputAreaCode     |                               |
		| MiddleSuperOutputAreaName     |                               |
		| NavVendorNo                   |                               |
		| ParliamentaryConstituencyCode |                               |
		| ParliamentaryConstituencyName |                               |
		| PhaseOfEducation              |                               |
		| Postcode                      |                               |
		| ProviderProfileIdType         |                               |
		| ProviderType                  | Academies                     |
		| ProviderSubType               | Academy special sponsor led   |
		| ProviderVersionId             | <ProviderVersionId>           |
		| ReasonEstablishmentClosed     |                               |
		| ReasonEstablishmentOpened     |                               |
		| RscRegionCode                 |                               |
		| RscRegionName                 |                               |
		| Status                        |                               |
		| Successor                     |                               |
		| Town                          |                               |
		| TrustCode                     | 1001                          |
		| TrustName                     |                               |
		| TrustStatus                   | SupportedByAMultiAcademyTrust |
		| UKPRN                         | 1000102                       |
		| UPIN                          |                               |
		| URN                           |                               |
		| WardCode                      |                               |
		| WardName                      |                               |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 1000000                  |
		| Name                          | Maintained School 1      |
		| Authority                     | Local Authority 1        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2012-03-15               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 200                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Maintained School 1      |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000000                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	And the provider with id '1000000' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 1000002                  |
		| Name                          | Maintained School 2      |
		| Authority                     | Local Authority 1        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2013-04-16               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 200                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Local Authority 1        |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000002                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	And the provider with id '1000002' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 1000003                  |
		| Name                          | Maintained School 3      |
		| Authority                     | Local Authority 1        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2013-04-16               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 200                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Local Authority 1        |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000003                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	And the provider with id '1000003' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 1000004                  |
		| Name                          | Maintained School 4      |
		| Authority                     | Local Authority 2        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2013-04-16               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 202                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Local Authority 2        |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000004                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	And the provider with id '1000004' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 1000005                  |
		| Name                          | Maintained School 5      |
		| Authority                     | Local Authority 2        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2013-04-16               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 202                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Local Authority 2        |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000005                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	And the provider with id '1000005' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                                                                       |
		| ProviderId                    | 1000009                                                                     |
		| Name                          | Maintained School 9  - Excluded for funding, but in scope for specification |
		| Authority                     | Local Authority 3                                                           |
		| CensusWardCode                |                                                                             |
		| CensusWardName                |                                                                             |
		| CompaniesHouseNumber          |                                                                             |
		| CountryCode                   |                                                                             |
		| CountryName                   |                                                                             |
		| CrmAccountId                  |                                                                             |
		| DateClosed                    |                                                                             |
		| DateOpened                    | 2013-04-16                                                                  |
		| DfeEstablishmentNumber        |                                                                             |
		| DistrictCode                  |                                                                             |
		| DistrictName                  |                                                                             |
		| EstablishmentNumber           |                                                                             |
		| GovernmentOfficeRegionCode    |                                                                             |
		| GovernmentOfficeRegionName    |                                                                             |
		| GroupIdNumber                 |                                                                             |
		| LACode                        | 203                                                                         |
		| LegalName                     |                                                                             |
		| LocalAuthorityName            | Local Authority 3                                                           |
		| LowerSuperOutputAreaCode      |                                                                             |
		| LowerSuperOutputAreaName      |                                                                             |
		| MiddleSuperOutputAreaCode     |                                                                             |
		| MiddleSuperOutputAreaName     |                                                                             |
		| NavVendorNo                   |                                                                             |
		| ParliamentaryConstituencyCode |                                                                             |
		| ParliamentaryConstituencyName |                                                                             |
		| PhaseOfEducation              |                                                                             |
		| Postcode                      |                                                                             |
		| ProviderProfileIdType         |                                                                             |
		| ProviderType                  | LA maintained schools                                                       |
		| ProviderSubType               | Community school                                                            |
		| ProviderVersionId             | <ProviderVersionId>                                                         |
		| ReasonEstablishmentClosed     |                                                                             |
		| ReasonEstablishmentOpened     |                                                                             |
		| RscRegionCode                 |                                                                             |
		| RscRegionName                 |                                                                             |
		| Status                        |                                                                             |
		| Successor                     |                                                                             |
		| Town                          |                                                                             |
		| TrustCode                     |                                                                             |
		| TrustName                     |                                                                             |
		| TrustStatus                   | Not Supported By A Trust                                                    |
		| UKPRN                         | 1000009                                                                     |
		| UPIN                          |                                                                             |
		| URN                           |                                                                             |
		| WardCode                      |                                                                             |
		| WardName                      |                                                                             |
	And the provider with id '1000009' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	# Academy providers
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                         |
		| ProviderId                    | 1000101                       |
		| Name                          | Academy 1                     |
		| Authority                     | Local Authority 1             |
		| CensusWardCode                |                               |
		| CensusWardName                |                               |
		| CompaniesHouseNumber          |                               |
		| CountryCode                   |                               |
		| CountryName                   |                               |
		| CrmAccountId                  |                               |
		| DateClosed                    |                               |
		| DateOpened                    | 2013-04-16                    |
		| DfeEstablishmentNumber        |                               |
		| DistrictCode                  |                               |
		| DistrictName                  |                               |
		| EstablishmentNumber           |                               |
		| GovernmentOfficeRegionCode    |                               |
		| GovernmentOfficeRegionName    |                               |
		| GroupIdNumber                 |                               |
		| LACode                        | 200                           |
		| LegalName                     |                               |
		| LocalAuthorityName            | Local Authority 1             |
		| LowerSuperOutputAreaCode      |                               |
		| LowerSuperOutputAreaName      |                               |
		| MiddleSuperOutputAreaCode     |                               |
		| MiddleSuperOutputAreaName     |                               |
		| NavVendorNo                   |                               |
		| ParliamentaryConstituencyCode |                               |
		| ParliamentaryConstituencyName |                               |
		| PhaseOfEducation              |                               |
		| Postcode                      |                               |
		| ProviderProfileIdType         |                               |
		| ProviderType                  | Academies                     |
		| ProviderSubType               | Academy special sponsor led   |
		| ProviderVersionId             | <ProviderVersionId>           |
		| ReasonEstablishmentClosed     |                               |
		| ReasonEstablishmentOpened     |                               |
		| RscRegionCode                 |                               |
		| RscRegionName                 |                               |
		| Status                        |                               |
		| Successor                     |                               |
		| Town                          |                               |
		| TrustCode                     | 1001                          |
		| TrustName                     |                               |
		| TrustStatus                   | SupportedByAMultiAcademyTrust |
		| UKPRN                         | 1000101                       |
		| UPIN                          |                               |
		| URN                           |                               |
		| WardCode                      |                               |
		| WardName                      |                               |
	And the provider with id '1000101' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                         |
		| ProviderId                    | 1000102                       |
		| Name                          | Academy 2                     |
		| Authority                     | Local Authority 1             |
		| CensusWardCode                |                               |
		| CensusWardName                |                               |
		| CompaniesHouseNumber          |                               |
		| CountryCode                   |                               |
		| CountryName                   |                               |
		| CrmAccountId                  |                               |
		| DateClosed                    |                               |
		| DateOpened                    | 2013-04-16                    |
		| DfeEstablishmentNumber        |                               |
		| DistrictCode                  |                               |
		| DistrictName                  |                               |
		| EstablishmentNumber           |                               |
		| GovernmentOfficeRegionCode    |                               |
		| GovernmentOfficeRegionName    |                               |
		| GroupIdNumber                 |                               |
		| LACode                        | 200                           |
		| LegalName                     |                               |
		| LocalAuthorityName            | Local Authority 1             |
		| LowerSuperOutputAreaCode      |                               |
		| LowerSuperOutputAreaName      |                               |
		| MiddleSuperOutputAreaCode     |                               |
		| MiddleSuperOutputAreaName     |                               |
		| NavVendorNo                   |                               |
		| ParliamentaryConstituencyCode |                               |
		| ParliamentaryConstituencyName |                               |
		| PhaseOfEducation              |                               |
		| Postcode                      |                               |
		| ProviderProfileIdType         |                               |
		| ProviderType                  | Academies                     |
		| ProviderSubType               | Academy special sponsor led   |
		| ProviderVersionId             | <ProviderVersionId>           |
		| ReasonEstablishmentClosed     |                               |
		| ReasonEstablishmentOpened     |                               |
		| RscRegionCode                 |                               |
		| RscRegionName                 |                               |
		| Status                        |                               |
		| Successor                     |                               |
		| Town                          |                               |
		| TrustCode                     | 1001                          |
		| TrustName                     |                               |
		| TrustStatus                   | SupportedByAMultiAcademyTrust |
		| UKPRN                         | 1000102                       |
		| UPIN                          |                               |
		| URN                           |                               |
		| WardCode                      |                               |
		| WardName                      |                               |
	And the provider with id '1000102' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                         |
		| ProviderId                    | 1000103                       |
		| Name                          | Academy 3                     |
		| Authority                     | Local Authority 2             |
		| CensusWardCode                |                               |
		| CensusWardName                |                               |
		| CompaniesHouseNumber          |                               |
		| CountryCode                   |                               |
		| CountryName                   |                               |
		| CrmAccountId                  |                               |
		| DateClosed                    |                               |
		| DateOpened                    | 2013-04-16                    |
		| DfeEstablishmentNumber        |                               |
		| DistrictCode                  |                               |
		| DistrictName                  |                               |
		| EstablishmentNumber           |                               |
		| GovernmentOfficeRegionCode    |                               |
		| GovernmentOfficeRegionName    |                               |
		| GroupIdNumber                 |                               |
		| LACode                        | 200                           |
		| LegalName                     |                               |
		| LocalAuthorityName            | Local Authority 2             |
		| LowerSuperOutputAreaCode      |                               |
		| LowerSuperOutputAreaName      |                               |
		| MiddleSuperOutputAreaCode     |                               |
		| MiddleSuperOutputAreaName     |                               |
		| NavVendorNo                   |                               |
		| ParliamentaryConstituencyCode |                               |
		| ParliamentaryConstituencyName |                               |
		| PhaseOfEducation              |                               |
		| Postcode                      |                               |
		| ProviderProfileIdType         |                               |
		| ProviderType                  | Free Schools                  |
		| ProviderSubType               | Free Schools                  |
		| ProviderVersionId             | <ProviderVersionId>           |
		| ReasonEstablishmentClosed     |                               |
		| ReasonEstablishmentOpened     |                               |
		| RscRegionCode                 |                               |
		| RscRegionName                 |                               |
		| Status                        |                               |
		| Successor                     |                               |
		| Town                          |                               |
		| TrustCode                     | 1002                          |
		| TrustName                     |                               |
		| TrustStatus                   | SupportedByAMultiAcademyTrust |
		| UKPRN                         | 1000103                       |
		| UPIN                          |                               |
		| URN                           |                               |
		| WardCode                      |                               |
		| WardName                      |                               |
	And the provider with id '1000103' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	# Local Authorities in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 9000000                  |
		| Name                          | Local Authority 1        |
		| Authority                     | Local Authority 1        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2012-03-15               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 200                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Local Authority 1        |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | Local Authority          |
		| ProviderSubType               | Local Authority          |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 9000000                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 9000002                  |
		| Name                          | Local Authority 2        |
		| Authority                     | Local Authority 2        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2012-03-15               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 202                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Local Authority 2        |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | Local Authority          |
		| ProviderSubType               | Local Authority          |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 9000002                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 9000003                  |
		| Name                          | Local Authority 3        |
		| Authority                     | Local Authority 3        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2012-03-15               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 202                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Local Authority 3        |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | Local Authority          |
		| ProviderSubType               | Local Authority          |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     |                          |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 9000003                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	# Academy Trusts
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 8000001                  |
		| Name                          | Academy Trust 1          |
		| Authority                     | Local Authority 1        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2012-03-15               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 202                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Local Authority 1        |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | Academy Trust            |
		| ProviderSubType               | Academy Trust            |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     | 1001                     |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 8000001                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 8000002                  |
		| Name                          | Academy Trust 2          |
		| Authority                     | Local Authority 2        |
		| CensusWardCode                |                          |
		| CensusWardName                |                          |
		| CompaniesHouseNumber          |                          |
		| CountryCode                   |                          |
		| CountryName                   |                          |
		| CrmAccountId                  |                          |
		| DateClosed                    |                          |
		| DateOpened                    | 2012-03-15               |
		| DfeEstablishmentNumber        |                          |
		| DistrictCode                  |                          |
		| DistrictName                  |                          |
		| EstablishmentNumber           |                          |
		| GovernmentOfficeRegionCode    |                          |
		| GovernmentOfficeRegionName    |                          |
		| GroupIdNumber                 |                          |
		| LACode                        | 202                      |
		| LegalName                     |                          |
		| LocalAuthorityName            | Academy Trust 1          |
		| LowerSuperOutputAreaCode      |                          |
		| LowerSuperOutputAreaName      |                          |
		| MiddleSuperOutputAreaCode     |                          |
		| MiddleSuperOutputAreaName     |                          |
		| NavVendorNo                   |                          |
		| ParliamentaryConstituencyCode |                          |
		| ParliamentaryConstituencyName |                          |
		| PhaseOfEducation              |                          |
		| Postcode                      |                          |
		| ProviderProfileIdType         |                          |
		| ProviderType                  | Academy Trust            |
		| ProviderSubType               | Academy Trust            |
		| ProviderVersionId             | <ProviderVersionId>      |
		| ReasonEstablishmentClosed     |                          |
		| ReasonEstablishmentOpened     |                          |
		| RscRegionCode                 |                          |
		| RscRegionName                 |                          |
		| Status                        |                          |
		| Successor                     |                          |
		| Town                          |                          |
		| TrustCode                     | 1002                     |
		| TrustName                     |                          |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 8000001                  |
		| UPIN                          |                          |
		| URN                           |                          |
		| WardCode                      |                          |
		| WardName                      |                          |
	When funding is published
	Then publishing succeeds
	And the following published funding is produced
	| Field                            | Value             |
	| GroupingReason                   | Payment           |
	| OrganisationGroupTypeCode        | LocalAuthority    |
	| OrganisationGroupIdentifierValue | 9000000           |
	| FUndingPeriodId                  | <FundingPeriodId> |
	| FundingStreamId                  | <FundingStreamId> |
	And the total funding is '36000'
	And the published funding contains the following published provider ids
	| FundingIds                                     |
	| <FundingStreamId>-<FundingPeriodId>-1000000-1_0 |
	| <FundingStreamId>-<FundingPeriodId>-1000002-1_0 |
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-1920' has the value of '21000'
	And the published funding contains a distribution period in funding line 'TotalAllocation' with id of 'FY-2021' has the value of '15000'

	Examples:
		| FundingStreamId | FundingPeriodId | FundingPeriodName     | TemplateVersion | ProviderVersionId |
		| PSG             | AY-1920         | Academic Year 2019-20 | 1.0             | psg-providers-1.0 |