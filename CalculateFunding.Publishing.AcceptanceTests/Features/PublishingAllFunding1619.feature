Feature: PublishingAllFunding1619
	In order to publish funding for Dedicated Schools Grant
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
		| Field     | Value               |
		| Id        | <FundingPeriodId>   |
		| Name      | <FundingPeriodName> |
		| StartDate | 2019-08-01 00:00:00 |
		| EndDate   | 2020-07-31 00:00:00 |
		| Period    | 2021                |
		| Type      | AS                  |
	And the following specification exists
		| Field                | Value                             |
		| Id                   | specForPublishing                 |
		| Name                 | Test Specification for Publishing |
		| IsSelectedForFunding | true                              |
		| ProviderVersionId    | <ProviderVersionId>               |
	And the specification has the funding period with id '<FundingPeriodId>' and name '<FundingPeriodName>'
	And the specification has the following funding streams
		| Name | Id                |
		| 1619  | <FundingStreamId> |
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
		| Name              | 1619 Provider Version |
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
		| Name                                                 | FundingLineCode | Value | TemplateLineId | Type        |
		| Pupil Led Factors                                    | 1619-004         | 640   | 5              | Information |
		| Funding Through Premises and Mobility Factors        | 1619-007         | 0     | 6              | Information |
		| Growth funding                                       | 1619-006         | 0     | 7              | Information |
		| CSSB Pupil Led Funding                               | 1619-012         | 320   | 23             | Information |
		| Funding for Historic Commitments                     | 1619-013         | 0     | 27             | Information |
		| Universal Entitlement for 3 and 4 Year Olds          | 1619-014         | 320   | 130            | Information |
		| Funding for Additional Hours for working parents     | 1619-015         | 320   | 135            | Information |
		| Participation Funding for Disadvantaged 2 Year Olds  | 1619-016         | 320   | 140            | Information |
		| Funding Allocation for Early Years Pupil Premium     | 1619-017         | 320   | 145            | Information |
		| Funding Allocation for Maintained Nursery Supplement | 1619-018         | 320   | 150            | Information |
		| Funding Allocation for Disability Access Fund        | 1619-019         | 0     | 155            | Information |
		| Schools Block before recoupment                      | 1619-003         | 640   | 4              | Information |
		| Central School Services Block                        | 1619-008         | 320   | 8              | Information |
		| Early Years Block                                    | 1619-009         | 1600  | 9              | Information |
		| High Needs Block before deductions                   | 1619-010         | 1280  | 10             | Information |
		| Total High Needs Block After Deductions              | 1619-011         | 14720 | 11             | Information |
		| School Block After recoupment                        | 1619-005         | 640   | 12             | Information |
		| Provisional Schools Block Funding Excluding Growth   | 1619-020         | 0     | 239            | Information |
		| LA Protection                                        | 1619-021         | 0     | 240            | Information |
		| Total 1619 before deductions and recoupment           | 1619-001         | 3840  | 2              | Information |
		| Total 1619 after deductions and recoupment            | 1619-002         | 15360 | 3              | Payment     |
	And the Published Provider has the following distribution period for funding line '1619-002'
		| DistributionPeriodId | Value |
		| AS-1920              | 7000  |
		| AS-2021              | 5000  |
	And the Published Providers distribution period has the following profiles for funding line '1619-002'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AS-1920              | CalendarMonth | October   | 1920 | 1          | 7000          |
		| AS-2021              | CalendarMonth | April     | 2021 | 1          | 5000          |
	And template mapping exists
		| EntityType  | CalculationId                        | TemplateId | Name                                                                                                                                                   |
		| Calculation | e46c0b59-6900-4fae-90b5-da6e558d9491 | 15         | Primary Pupil Number                                                                                                                                   |
		| Calculation | dd54d574-f9ef-4bef-bf82-1cb31c526f36 | 16         | Primary Unit of Funding                                                                                                                                |
		| Calculation | ff97843a-5fa9-4435-81c3-c44785611b01 | 17         | Secondary Pupil Number                                                                                                                                 |
		| Calculation | b7c8bac7-c470-4b9b-af8f-0e4bd9886742 | 18         | Secondary Unit of Funding                                                                                                                              |
		| Calculation | f43ba392-bf58-4677-a2c4-87ac114498fe | 13         | Primary pupil sub-total                                                                                                                                |
		| Calculation | 54dea110-7fb7-41c0-bf87-2050ad54a042 | 14         | Secondary pupil sub-total                                                                                                                              |
		| Calculation | c71aa30e-73a4-4b5e-b1f3-61856b92b1fd | 19         | Premises and Mobility Factors                                                                                                                          |
		| Calculation | 21c65d92-31a8-4b08-9a78-caada398b8d2 | 20         | Growth                                                                                                                                                 |
		| Calculation | a130170a-b70e-463c-960d-639e2155ca3e | 25         | CSSB per Pupil Rate                                                                                                                                    |
		| Calculation | fcd312aa-c9bb-4b1f-97b2-2efeaccc6c5a | 26         | CSSB Pupil Number                                                                                                                                      |
		| Calculation | bd784ee0-e837-4859-8891-1f991d429657 | 24         | CSSB Pupil Led Total                                                                                                                                   |
		| Calculation | e79438f4-be66-4653-b61b-a2f7233c4549 | 28         | Actual CSSB Funding for Historic Commitments                                                                                                           |
		| Calculation | 7709b5c0-699f-455e-8773-7b258d801792 | 132        | Universal Entitlement for 3 and 4 Year Olds total Early Years Universal Entitlement for 3 and 4 Year Olds Rate                                         |
		| Calculation | 6d1c482b-6df9-4ba4-842e-704aa5f6100f | 133        | Total 3 and 4 Year Olds (PTE)                                                                                                                          |
		| Calculation | 771727d2-61a7-4928-b9c5-71e80ace0c4d | 134        | Universal Entitlement for 3 and 4 Year Olds total PTE Funded hours                                                                                     |
		| Calculation | f5afea39-d21c-443b-859a-595c78789989 | 131        | Universal Entitlement for 3 and 4 Year Olds total                                                                                                      |
		| Calculation | 36df4068-f3e0-4040-a73f-40dc66e1bad0 | 137        | Funding for Additional Hours for working parents total Early Years Universal Entitlement for 3 and 4 Year Olds Rate                                    |
		| Calculation | 989fe1d5-9913-49d8-ba07-5685052e818f | 138        | Total 3 and 4 Year Old for Additional Hours for Working Parents (PTE)                                                                                  |
		| Calculation | 704db23d-cf70-4a14-93b4-c966727cedb0 | 139        | Funding for Additional Hours for working parents total PTE Funded hours                                                                                |
		| Calculation | df645907-2cf0-4f25-99a4-4147ac62e1df | 136        | Funding for Additional Hours for working parents total                                                                                                 |
		| Calculation | 300dbe4d-b008-4ede-a189-4b456fac6870 | 142        | Early Years Participation Funding for 2 Year Olds Rate                                                                                                 |
		| Calculation | 96d3e984-ccf5-440e-960a-fcbea5034e74 | 143        | Total 2 Year Olds (PTE)                                                                                                                                |
		| Calculation | ddfdaf6f-0d81-4655-b71e-2c8b9320329c | 144        | Participation Funding for Disadvantaged 2 Year Olds total PTE Funded hours                                                                             |
		| Calculation | 030997f5-38c6-453e-a40a-5f640d2bf223 | 141        | Participation Funding for Disadvantaged 2 Year Olds total                                                                                              |
		| Calculation | e616e94c-bdf9-4746-aba9-cbe3ee7b27cf | 147        | Early Years Pupil Premium                                                                                                                              |
		| Calculation | ccacff9b-1767-4045-82ff-669d56c018ca | 148        | Pupil Premium Pupil Count for 3 to 4 Year Olds (PTE)                                                                                                   |
		| Calculation | 18274db3-2c2d-4c2d-a46c-17cc5d98737d | 149        | Funding Allocation for Early Years Pupil Premium total PTE Funded hours                                                                                |
		| Calculation | 644667cc-c636-47b9-ade8-e2f7af501388 | 146        | Funding Allocation for Early Years Pupil Premium total                                                                                                 |
		| Calculation | c14a47a0-3a88-4dd9-9bd1-a5935e0cb49b | 152        | Maintained Nursery Schools Supplement Hourly Rate                                                                                                      |
		| Calculation | 4a4a6b45-a540-4efa-be10-8188d04460fe | 153        | Maintained Nursery Schools Supplement (PTE)                                                                                                            |
		| Calculation | a53c47a9-b181-4158-902c-abe74efeccfa | 154        | PTE Funded hours                                                                                                                                       |
		| Calculation | 67e747b8-1d25-4de0-8101-7f81e8d7c051 | 151        | Funding Allocation for Maintained Nursery Supplement total                                                                                             |
		| Calculation | b7ec40dc-1f24-4b69-b0dc-ecc4a7f9eb5c | 31         | Basic Entitlement Per Pupil Rate                                                                                                                       |
		| Calculation | b64dbf4f-5ac4-4ad3-91cd-c98ce40c606a | 32         | Basic Entitlement Per Pupil Number                                                                                                                     |
		| Calculation | d3704649-3119-42e1-ab69-b1758f2c0fd5 | 34         | Import/Export Adjustment per pupil number                                                                                                              |
		| Calculation | 3647fb00-31d9-4c3a-a20a-e8631810dd55 | 35         | Import/Export Adjustment per pupil rate                                                                                                                |
		| Calculation | 49494307-c1a1-40a5-a2d5-38d88bca8ede | 37         | Mid 2019 Age 2-18 ONS Population Projection                                                                                                            |
		| Calculation | 5f19e35b-f532-43e7-9133-638a29324815 | 38         | Additional High Needs Funding Quantum                                                                                                                  |
		| Calculation | 9c0f0435-5268-49c0-a0a7-74f8d477a52a | 29         | HN Block Baseline                                                                                                                                      |
		| Calculation | fae774f5-f73c-4bd1-8fc8-aee66fb51c0c | 237        | Additional Funding For free Schools                                                                                                                    |
		| Calculation | 61709649-863e-4a1f-b03b-ddcac25625a4 | 30         | Basic Entitlement                                                                                                                                      |
		| Calculation | b9dd905a-8aa4-41ee-8f90-f0d655f3553f | 33         | Import/Export Adjustment                                                                                                                               |
		| Calculation | 9756e012-9f5e-47c9-a5b8-c1c7188b6829 | 36         | Additonal High Needs Funding                                                                                                                           |
		| Calculation | d95c681b-66f4-49e5-bf6e-22f330afe3b4 | 65         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£6k SEN places deduction April (Year 1) - August (Year 1)                 |
		| Calculation | 0b643470-b859-4a5e-b2cd-f45a33657466 | 66         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£6k SEN places deduction September (Year 1) - March (Year 2)              |
		| Calculation | 4c0eaa57-642e-4fba-9c75-4174c4be0ab4 | 67         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£6k SEN places deduction April (Year 1) - August (Year 1) rate            |
		| Calculation | 4543e33f-8b40-4335-901d-834ab84b761f | 68         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£6k SEN places deduction September (Year 1) - March (Year 2) rate         |
		| Calculation | 2375efb5-4275-4f64-96bf-01c35b4dfc54 | 64         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£6k SEN places deduction                                                  |
		| Calculation | 07400efc-cd8a-4062-bdde-3dac44bce704 | 70         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£10k SEN places deduction April (Year 1) - August (Year 1)                |
		| Calculation | b4290464-36ed-42ca-9df9-822003200b9d | 71         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£10k SEN places deduction September (Year 1) - March (Year 2)             |
		| Calculation | 8f967c9a-40e2-4842-8c55-969abf5a350f | 235        | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£10k SEN places deduction April (Year 1) - August (Year 1) Top Up rate    |
		| Calculation | 6bb71c56-818a-4ab1-b35f-ee497378ae3d | 236        | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£10k SEN places deduction September (Year 1) - March (Year 2) Top Up rate |
		| Calculation | 42919210-5ab6-4f98-8cbd-36ea585bfc3e | 241        | Provisional Schools Block Funding Excluding Growth                                                                                                     |
		| Calculation | 50d71379-e1e6-4f9a-8a82-8d8697049662 | 242        | Current Year School Block Pupil No                                                                                                 |
		| Calculation | ac233126-aa00-470a-a2db-aed95c7bead5 | 243        | Current Year Local Authority Protection																												   |
		| Calculation | b7fd2f32-1e43-4b3b-84f9-9754f71a0E9f | 244        | Percentage Change between Current Year and Previous Year per Pupil Funding																			   |
		| Calculation | b5f7f814-e819-4efb-9c07-e6b973a5dc30 | 245        | Percentage Change between Current Year and Previous Year per Pupil Funding after local authority protection											   |
		| Calculation | 1d05db55-d225-49a3-9240-fce6e9065de5 | 246        | Current Year Provisional Schools Block Excluding Growth Funding Per Pupil																			   |
		| Calculation | c36e9b84-8c83-45e6-8ccf-1d06c83d96d1 | 247        | Previous Year Schools Block Excluding Growth Funding Per Pupil                                                                                                     |
		| Calculation | 1f72e2d2-43fc-45df-b3e2-03a1d5E7d6d7 | 248        | Current Year Provisional Schools Block Funding Excluding Growth - NON CASH                                                                                                     |
		| Calculation | b2fc456d-7191-4214-9db5-1561cd3f75fa | 249        | Previous Year Schools Block Pupil Numbers                                                                                                     |
		| Calculation | f8b65554-b578-4041-a9F6-116830cc0a2b | 250        | Previous Year Schools Block Excluding Growth Funding                                                                                                   |
		| Calculation | bd4b64d4-f61f-4207-a493-73cb8d1e0db8 | 72         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£10k SEN places deduction April (Year 1) - August (Year 1) rate           |
		| Calculation | bd3ef104-827f-42a0-a1c0-365bc5c21156 | 73         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£10k SEN places deduction September (Year 1) - March (Year 2) rate        |
		| Calculation | 2794481e-5514-49fd-bcb7-0a91b0d40647 | 69         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£10k SEN places deduction                                                 |
		| Calculation | 16225034-ba94-4ecd-bb26-703c1ed4df29 | 75         | Mainstream Academies (SEN units and Resourced provision) Post-16 SEN Places Deduction April (Year 1) - July (Year 1)                                   |
		| Calculation | a9072025-e28a-4302-94d7-46e5a48acf38 | 76         | Mainstream Academies (SEN units and Resourced provision) Post-16 SEN Places Deduction August (Year 1) - March (Year 2)                                 |
		| Calculation | 01b34a35-4929-4896-8a25-6e9ccdf38607 | 77         | Mainstream Academies (SEN units and Resourced provision) Post-16 SEN Places Deduction April (Year 1) - July (Year 1) rate                              |
		| Calculation | e7c1cedf-254f-4635-8f71-e2c6688eb7f0 | 78         | Mainstream Academies (SEN units and Resourced provision) Post-16 SEN Places Deduction August (Year 1) - March (Year 2) rate                            |
		| Calculation | d7219481-eb38-48f4-a6c2-5775b6d8833e | 74         | Mainstream Academies (SEN units and Resourced provision) Post-16 SEN Places Deduction                                                                  |
		| Calculation | fdc6128c-4dca-456d-807d-252e93a2bdca | 80         | Mainstream Academies (SEN units and Resourced provision) Pre-16 AP Places Deduction April (Year 1) - August (Year 1)                                   |
		| Calculation | 7b9dd9be-e63c-4223-ba2d-2a7593e4b3ee | 81         | Mainstream Academies (SEN units and Resourced provision) Pre-16 AP Places Deduction September (Year 1) - March (Year 2)                                |
		| Calculation | d53b992a-5484-4d9f-91d7-aa4cf297e9b5 | 82         | Mainstream Academies (SEN units and Resourced provision) Pre-16 AP Places Deduction April (Year 1) - August (Year 1) rate                              |
		| Calculation | 766fd82c-bea2-476f-805f-ede2d6a662f0 | 83         | Mainstream Academies (SEN units and Resourced provision) Pre-16 AP Places Deduction September (Year 1) - March (Year 2) rate                           |
		| Calculation | 1972f183-a4ea-4f91-b064-1a2f9d11eb71 | 79         | Mainstream Academies (SEN units and Resourced provision) Pre-16 AP Places Deduction                                                                    |
		| Calculation | b8601c5c-f3eb-45d3-abfe-84120b8ff371 | 50         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£6k                                                                       |
		| Calculation | 47b47407-00c2-4888-9c3d-cb22b6e62384 | 51         | Mainstream Academies (SEN units and Resourced provision) Pre-16 SEN Places @Â£10k                                                                      |
		| Calculation | 3ba563a7-4611-4c2e-9951-1595969bf9d2 | 52         | Post-16 SEN Places Main Stream Academies                                                                                                               |
		| Calculation | ca896c28-5b68-4f36-a699-42da4640a36d | 53         | Mainstream Academies (SEN units and Resourced provision) Pre-16 AP Places                                                                              |
		| Calculation | f68fa739-4eea-4ba3-9e49-5e96d23731bc | 163        | Special Academies Pre-16 SEN Places Deduction April (Year 1) - July (Year 1)                                                                           |
		| Calculation | 33b8ea92-2344-4429-b28a-1cff65745e6f | 164        | Special Academies Pre-16 SEN Places Deduction August (Year 1) - March (Year 2)                                                                         |
		| Calculation | 108caf20-de82-49e1-8779-48f0326807dc | 165        | Special Academies Pre-16 SEN Places Deduction April (Year 1)Â Â - July (Year 1) rate                                                                   |
		| Calculation | ddb3eb07-917a-4080-b338-98e985c21ec8 | 166        | Special Academies Pre-16 SEN Places Deduction August (Year 1) - March (Year 2) rate                                                                    |
		| Calculation | f89336ae-1ae7-44b1-8b21-306697c8a2ad | 162        | Special Academies Deduction                                                                                                                            |
		| Calculation | f9c77603-cc44-497c-8ba4-3d41dbd14c26 | 90         | Special Academies Post-16 SEN Places Deduction April (Year 1) - July (Year 1)                                                                          |
		| Calculation | f2b7cf2a-495d-4157-ac97-789f86a9073a | 91         | Special Academies Post-16 SEN Places Deduction August (Year 1) - March (Year 2)                                                                        |
		| Calculation | 9491613b-b7a2-42ef-b590-51dd003d55c4 | 92         | Special Academies Post-16 SEN Places Deduction April (Year 1)Â Â - July (Year 1) rate                                                                  |
		| Calculation | d896cdd6-4b3f-4136-81e5-e892b8468b02 | 93         | Special Academies Post-16 SEN Places Deduction August (Year 1) - March (Year 2) rate                                                                   |
		| Calculation | 49ac5375-2c3f-4d5e-88f9-6f35be9e251b | 89         | Special Academies Post-16 SEN Places Deduction                                                                                                         |
		| Calculation | f8bdcb90-cfdc-424f-bfab-6e829cfe9140 | 95         | Special Academies Pre-16 AP Places Deduction April (Year 1) - July (Year 1)                                                                            |
		| Calculation | 746b26a3-9077-4bc5-8b3e-6a562ccb66de | 96         | Special Academies Pre-16 AP Places Deduction August (Year 1) - March (Year 2)                                                                          |
		| Calculation | d0e4f8b7-1fba-4edd-9ed6-30c92e509584 | 97         | Special Academies Pre-16 AP Places Deduction April (Year 1)Â Â - July (Year 1) rate                                                                    |
		| Calculation | a4ec12da-02ba-4103-816d-296e7156c5b0 | 98         | Special Academies Pre-16 AP Places Deduction August (Year 1) - March (Year 2) rate                                                                     |
		| Calculation | d758185f-b7b3-4ddf-a63f-3f68c1ceb18e | 94         | Special Academies Pre-16 AP Places Deduction                                                                                                           |
		| Calculation | da3f8c8b-9d51-4ac2-89bf-ce3875e6c073 | 158        | Special Academies Pre-16 SEN Places                                                                                                                    |
		| Calculation | 3973e082-7248-44c0-89b1-7a1939927ab6 | 175        | Special Academies Post-16 SEN Places                                                                                                                   |
		| Calculation | 1d818411-fb19-4589-b541-d3fb3b941d52 | 174        | Special AcademiesÂ Pre-16 AP Places                                                                                                                    |
		| Calculation | 161c81cc-1412-4c9d-aecf-0688f6075323 | 168        | Special Free Schools Pre-16 SEN Places Deduction April (Year 1) - August (Year 1)                                                                      |
		| Calculation | deddfe8a-6f21-405d-8f8e-b3d77004785b | 169        | Special Free Schools Pre-16 SEN Places Deduction August (Year 1) - March (Year 2)                                                                      |
		| Calculation | c93608d3-58d7-4a6d-89ec-ba2c1fb77fb6 | 170        | Special Free Schools Pre-16 SEN Places Deduction April (Year 1)Â - August (Year 1) rate                                                                |
		| Calculation | 85d6df27-22b5-4172-ac63-c03edb954ea6 | 171        | Special Free Schools Pre-16 SEN Places Deduction August (Year 1) - March (Year 2) rate                                                                 |
		| Calculation | a0320c8b-9dae-4719-9708-70076cb240f1 | 167        | Special Free Schools Deduction                                                                                                                         |
		| Calculation | c437b189-b105-469f-b3a0-2e2ccbddc989 | 176        | Special Free Schools Post-16 SEN Places Deduction April (Year 1) - July (Year 1)                                                                       |
		| Calculation | 89167b75-49b1-4d2a-b3ee-70bae9242317 | 177        | Special Free Schools Post-16 SEN Places Deduction August (Year 1) - March (Year 2)                                                                     |
		| Calculation | 0be04e61-d828-46df-b903-20f1e7d49a0d | 178        | Special Free Schools Post-16 SEN Places Deduction April (Year 1) - July (Year 1) rate                                                                  |
		| Calculation | 0ade4f3f-8ce0-4a1a-8499-52272d673b74 | 179        | Special Free Schools Post-16 SEN Places Deduction August (Year 1) - March (Year 2) rate                                                                |
		| Calculation | 9c2a3458-9c47-4634-8b01-c61aef671c65 | 232        | Special Free Schools Post-16 SEN Places Deduction                                                                                                      |
		| Calculation | fe1d36c8-4d60-45e0-afd9-25e81d4de265 | 182        | Special Free Schools Pre-16 AP Places Deduction April (Year 1) - August (Year 1)                                                                       |
		| Calculation | 54ae55fe-5c09-4393-a9ef-83a13d45439e | 183        | Special Free Schools Pre-16 AP Places Deduction September (Year 1) - March (Year 2)                                                                    |
		| Calculation | 6e8bb519-2f0a-4e43-98c6-cb8a4b0e438c | 180        | Special Free Schools Pre-16 AP Places Deduction April (Year 1)Â - August (Year 1) rate                                                                 |
		| Calculation | 870b5a27-4217-4b6e-bce3-9e5c823adbe1 | 181        | Special Free Schools Pre-16 AP Places Deduction September (Year 1) - March (Year 2) rate                                                               |
		| Calculation | f55b3de5-55e6-443e-8aa3-0e14f1f38eda | 234        | Special Free Schools Pre-16 AP Places Deduction                                                                                                        |
		| Calculation | 4d661833-9e57-4535-81fc-6de279af494f | 159        | Special Free Schools Pre-16 SEN Places                                                                                                                 |
		| Calculation | e92d3c27-3d11-45ce-aaf5-dbbdd9f672c5 | 161        | Special Free Schools Post-16 SEN Places                                                                                                                |
		| Calculation | 8dbc440e-5cef-45ec-ba6c-c7625f118136 | 233        | Special Free SchoolsÂ Pre-16 AP Places                                                                                                                 |
		| Calculation | 95eb6ce5-caad-478d-bbc6-2e913e4dd727 | 189        | AP Academies and Free Schools* Pre-16 SEN Places April (Year 1) - August (Year 1)                                                                      |
		| Calculation | c3962fe9-9545-427e-ae69-0719c87971eb | 190        | AP Academies and Free Schools* Pre-16 SEN Places Sept (Year 1) - March (Year 2)                                                                        |
		| Calculation | 841dd7e3-c39a-40eb-a80e-b380be8b5459 | 187        | AP Academies and Free Schools* April (Year 1) - August (Year 1) rate                                                                                   |
		| Calculation | 17ba6093-fc27-4d08-8e41-981e1a910bda | 188        | AP Academies and Free Schools* September (Year 1) - March (Year 2) rate                                                                                |
		| Calculation | aabee9ce-4e36-461d-b166-b8b794bac007 | 186        | AP Academies and Free Schools* Pre-16 SEN Places Deduction                                                                                             |
		| Calculation | b700cbcd-5594-4bac-ae2e-07a179f40830 | 195        | AP Academies & Free schools * Pre-16 AP Places Deduction April (Year 1) - August (Year 1)                                                              |
		| Calculation | 1993ff13-6371-4d78-8e23-cf8e37f051db | 196        | AP Academies & Free schools * Pre-16 AP Places Deduction September (Year 1) - March (Year 2)                                                           |
		| Calculation | 83e7f9cb-fff2-43b7-a81e-c8af459d4e54 | 193        | AP Academies & Free schools * Pre-16 AP Places Deduction April (Year 1) - August (Year 1) rate                                                         |
		| Calculation | ea07237f-3f20-4590-9184-2f9426a31c0a | 194        | AP Academies & Free schools * Pre-16 AP Places Deduction September (Year 1) - March (Year 2) rate                                                      |
		| Calculation | 0ca7fdb5-b1ba-4cb3-80a9-c71f541c3802 | 192        | AP Academies & Free schools * Pre-16 AP Places Deduction                                                                                               |
		| Calculation | bf3a8703-e7f9-4f91-8466-b7e367cf6a3e | 185        | AP Academies & Free schools * Pre- 16 SEN places                                                                                                       |
		| Calculation | 7226a3f2-e085-45d4-bfad-3d424d81644f | 191        | AP Academies & Free schools * Pre-16 AP Places                                                                                                         |
		| Calculation | 778ab5d8-c374-49b1-893e-9542e4b2c682 | 202        | Maintained Special Schools Post-16 SEN Places Deduction April (Year 1) - July (Year 1)                                                                 |
		| Calculation | 008e0d05-2aa1-4ea8-b609-c929654bf864 | 203        | Maintained Special Schools Post-16 SEN Places Deduction August (Year 1) - March (Year 2)                                                               |
		| Calculation | 67d4c440-649c-4c41-ad02-42b0f2d2d2f3 | 200        | Maintained Special Schools Post-16 SEN Places Deduction April (Year 1) - July (Year 1) rate                                                            |
		| Calculation | 07e3104b-43b4-43cb-9cd3-15a696847603 | 201        | Maintained Special Schools Post-16 SEN Places Deduction August (Year 1) - March (Year 1) rate                                                          |
		| Calculation | 1edc3242-5e80-4e2e-808f-eb73545daaee | 199        | Maintained Special Schools Post-16 SEN Places deduction                                                                                                |
		| Calculation | 420dd58c-cf5e-4d36-9dd2-8f3118011333 | 198        | Maintained Special Schools Post-16 SEN Places                                                                                                          |
		| Calculation | 8389a997-4145-43e5-94d6-d1cb4c72e6ef | 209        | Maintained Mainstream Schools Post-16 SEN Places Deduction April (Year 1) - July (Year 1)                                                              |
		| Calculation | df34bbaf-09ca-49df-8b64-d49d90bd1523 | 210        | Maintained Mainstream Schools Post-16 SEN Places Deduction August (Year 1) - March (Year 2)                                                            |
		| Calculation | 9567eecd-f7a7-48b5-b11e-38877ccc6603 | 207        | Maintained Mainstream Schools Post-16 SEN Places Deduction April (Year 1) - July (Year 1) rate                                                         |
		| Calculation | 57d5a373-0f6e-43d0-9164-d780316fef9a | 208        | Maintained Mainstream Schools Post-16 SEN Places Deduction August (Year 1) - March (Year 2) rate                                                       |
		| Calculation | ee2011d2-864d-47d3-b19f-fadd4f9d21be | 206        | Maintained Mainstream Schools Post-16 SEN Places Deduction                                                                                             |
		| Calculation | 8618821c-e21f-47e5-a97d-65973459fbab | 205        | Maintained Mainstream Schools Post-16 SEN Places                                                                                                       |
		| Calculation | 7556232a-1ad7-42ad-b766-a27ce3dba4e2 | 214        | Hospital Academies Funding Total Hospital Education Deduction April (Year 1) - August (Year 1)                                                         |
		| Calculation | a7e1682f-8004-4bc8-a720-2f564416a725 | 215        | Hospital Academies Funding Total Hospital Education Deduction September (Year 1) - March (Year 2)                                                      |
		| Calculation | e47315c0-2114-4756-9ee8-0d0ec44c5771 | 213        | Hospital Academies Funding Total Hospital Education Deduction                                                                                          |
		| Calculation | 0da63f9a-d7cd-4cf2-8db3-0b5a678040ab | 212        | Hospital Academies Funding Total Hospital Education                                                                                                    |
		| Calculation | 01e00072-a670-4bee-b0c8-53454460027e | 223        | 16-19 Academies and Free Schools Total Post-16 Schools SEN Places Deduction April (Year 1) to July (Year 1)                                            |
		| Calculation | 4a47a382-cb38-4bcc-9b22-897c092084c7 | 224        | 16-19 Academies and Free Schools Total Post-16 Schools SEN Places Deduction August (Year 1) to March (Year 2)                                          |
		| Calculation | f41f06bc-6717-4791-bfc7-aa4fe8981b7d | 221        | 16-19 Academies and Free Schools Total Post-16 Schools SEN Places Deduction April (Year 1) to July (Year 1) rate                                       |
		| Calculation | e539ecee-8ae6-4ce1-9e1f-453a888940f0 | 222        | 16-19 Academies and Free Schools Total Post-16 Schools SEN Places Deduction August (Year 1) to March (Year 2) rate                                     |
		| Calculation | a44ba971-8d73-4d65-85ff-8f04aa963f3d | 220        | 16-19 Academies and Free Schools Total Post-16 Schools SEN Places Deduction                                                                            |
		| Calculation | ca6d0788-8c26-4e02-be57-52b47ef72267 | 128        | FE and ILP Places April 2019 - July 2019                                                                                                               |
		| Calculation | c1faf3ac-759e-4455-8b01-bef377274e08 | 129        | FE and ILP Places August 2019- March 2020                                                                                                              |
		| Calculation | 8e2364b5-258f-4955-88ea-a5db1c958cad | 228        | FE and ILP Places Deduction April (Year 1) - July (Year 1) rate                                                                                        |
		| Calculation | a78dac8f-3ccf-42af-a137-719202a52730 | 229        | FE and ILP Places Deduction August (Year 1) - March (Year 2) rate                                                                                      |
		| Calculation | 1b91566c-ccab-42b5-84fc-b71b47c9e7bc | 227        | FE and ILP Deduction                                                                                                                                   |
		| Calculation | 2c245fa9-a9d6-46e6-a03a-41e2123f40d1 | 226        | FE and ILP Places                                                                                                                                      |
		| Calculation | 413ffcef-bb79-41a6-8c9a-bd11c7db9f70 | 42         | Mainstream Academies (SEN units and Resourced provision)                                                                                               |
		| Calculation | 372b42ed-5e9e-4e7b-99be-6c14b9def29b | 156        | Special Academies                                                                                                                                      |
		| Calculation | 28ffe367-6171-43a5-830c-24925ff63d06 | 157        | Special Free Schools                                                                                                                                   |
		| Calculation | d04fbcf8-4221-462c-b045-9e124b3775c2 | 184        | AP Academies & Free schools *                                                                                                                          |
		| Calculation | 18376b64-620a-4c34-ad44-6579013a58e6 | 197        | Maintained Special Schools                                                                                                                             |
		| Calculation | 11f6832f-5abb-45cb-9afe-539788754a72 | 204        | Maintained Mainstream Schools                                                                                                                          |
		| Calculation | 780d80b5-38e3-421f-90fc-9f4a351df76f | 211        | Hospital Academies                                                                                                                                     |
		| Calculation | c4f37785-819b-4680-9427-ce8f887285f4 | 218        | 16-19 Academies and Free Schools                                                                                                                       |
		| Calculation | dfe0db99-6652-46fd-8a90-6efe74642f55 | 225        | FE and ILP                                                                                                                                             |
		| Calculation | 683d948a-1aa5-4e90-b5fc-205d040772b8 | 40         | HN Deductions                                                                                                                                          |
		| Calculation | 58a67753-b104-455d-ae5d-fd303fe5b18d | 41         | HN before deductions                                                                                                                                   |
		| Calculation | b03fbd41-8454-4a4e-a9d1-e6cdfd4eeb60 | 39         | HN after deductions                                                                                                                                    |
		| Calculation | 8277b567-6d82-4760-95d3-56e26d31704e | 21         | Recoupment                                                                                                                                             |
		| Calculation | 82a82df7-a4e3-4d5d-b378-23af3607ceec | 22         | School Block After Recoupment                                                                                                                          |
		| Calculation | 1c066b09-3e98-48fc-966f-ea3f09401b54 | 238        | Disability Access Fund                                                                                                                                 |
		| Calculation | 4afd7426-1787-4b71-a5f7-971db86811be | 216        | Hospital Academies Funding Total Hospital Education Deduction April (Year 1) - August (Year 1) rate                                                    |
		| Calculation | 8fdd4341-88ea-47f2-ba81-511951ca7efd | 217        | Hospital Academies Funding Total Hospital Education Deduction September (Year 1) - March (Year 2) rate                                                 |
		| Calculation | 5cfb28de-88d6-4faa-a936-d81a065fb596 | 219        | 16-19 Academies and Free Schools Total Post-16 Schools SEN Places                                                                                      |

	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 238                   | 320   |
		| 15                    | 320   |
		| 16                    | 320   |
		| 17                    | 320   |
		| 18                    | 320   |
		| 13                    | 320   |
		| 14                    | 320   |
		| 19                    | 320   |
		| 20                    | 320   |
		| 25                    | 320   |
		| 26                    | 320   |
		| 24                    | 320   |
		| 28                    | 320   |
		| 132                   | 320   |
		| 133                   | 320   |
		| 134                   | 320   |
		| 131                   | 320   |
		| 137                   | 320   |
		| 138                   | 320   |
		| 139                   | 320   |
		| 136                   | 320   |
		| 142                   | 320   |
		| 143                   | 320   |
		| 144                   | 320   |
		| 141                   | 320   |
		| 147                   | 320   |
		| 148                   | 320   |
		| 149                   | 320   |
		| 146                   | 320   |
		| 152                   | 320   |
		| 153                   | 320   |
		| 154                   | 320   |
		| 151                   | 320   |
		| 31                    | 320   |
		| 32                    | 320   |
		| 34                    | 320   |
		| 35                    | 320   |
		| 37                    | 320   |
		| 38                    | 320   |
		| 29                    | 320   |
		| 237                   | 320   |
		| 30                    | 320   |
		| 33                    | 320   |
		| 36                    | 320   |
		| 65                    | 320   |
		| 66                    | 320   |
		| 67                    | 320   |
		| 68                    | 320   |
		| 64                    | 320   |
		| 70                    | 320   |
		| 71                    | 320   |
		| 235                   | 320   |
		| 236                   | 320   |
		| 241                   | 320   |
		| 242                   | 320   |
		| 243                   | 320   |
		| 244                   | 320   |
		| 245                   | 320   |
		| 246                   | 320   |
		| 247                   | 320   |
		| 248                   | 320   |
		| 249                   | 320   |
		| 250                   | 320   |
		| 72                    | 320   |
		| 73                    | 320   |
		| 69                    | 320   |
		| 75                    | 320   |
		| 76                    | 320   |
		| 77                    | 320   |
		| 78                    | 320   |
		| 74                    | 320   |
		| 80                    | 320   |
		| 81                    | 320   |
		| 82                    | 320   |
		| 83                    | 320   |
		| 79                    | 320   |
		| 50                    | 320   |
		| 51                    | 320   |
		| 52                    | 320   |
		| 53                    | 320   |
		| 163                   | 320   |
		| 164                   | 320   |
		| 165                   | 320   |
		| 166                   | 320   |
		| 162                   | 320   |
		| 90                    | 320   |
		| 91                    | 320   |
		| 92                    | 320   |
		| 93                    | 320   |
		| 89                    | 320   |
		| 95                    | 320   |
		| 96                    | 320   |
		| 97                    | 320   |
		| 98                    | 320   |
		| 94                    | 320   |
		| 158                   | 320   |
		| 175                   | 320   |
		| 174                   | 320   |
		| 168                   | 320   |
		| 169                   | 320   |
		| 170                   | 320   |
		| 171                   | 320   |
		| 167                   | 320   |
		| 176                   | 320   |
		| 177                   | 320   |
		| 178                   | 320   |
		| 179                   | 320   |
		| 232                   | 320   |
		| 182                   | 320   |
		| 183                   | 320   |
		| 180                   | 320   |
		| 181                   | 320   |
		| 234                   | 320   |
		| 159                   | 320   |
		| 161                   | 320   |
		| 233                   | 320   |
		| 189                   | 320   |
		| 190                   | 320   |
		| 187                   | 320   |
		| 188                   | 320   |
		| 186                   | 320   |
		| 195                   | 320   |
		| 196                   | 320   |
		| 193                   | 320   |
		| 194                   | 320   |
		| 192                   | 320   |
		| 185                   | 320   |
		| 191                   | 320   |
		| 202                   | 320   |
		| 203                   | 320   |
		| 200                   | 320   |
		| 201                   | 320   |
		| 199                   | 320   |
		| 198                   | 320   |
		| 209                   | 320   |
		| 210                   | 320   |
		| 207                   | 320   |
		| 208                   | 320   |
		| 206                   | 320   |
		| 205                   | 320   |
		| 214                   | 320   |
		| 215                   | 320   |
		| 213                   | 320   |
		| 223                   | 320   |
		| 224                   | 320   |
		| 221                   | 320   |
		| 222                   | 320   |
		| 220                   | 320   |
		| 212                   | 320   |
		| 128                   | 320   |
		| 129                   | 320   |
		| 228                   | 320   |
		| 229                   | 320   |
		| 227                   | 320   |
		| 226                   | 320   |
		| 42                    | 320   |
		| 156                   | 320   |
		| 157                   | 320   |
		| 184                   | 320   |
		| 197                   | 320   |
		| 204                   | 320   |
		| 211                   | 320   |
		| 218                   | 320   |
		| 225                   | 320   |
		| 40                    | 320   |
		| 41                    | 320   |
		| 39                    | 320   |
		| 21                    | 320   |
		| 22                    | 320   |
		| 216                   | 320   |
		| 217                   | 320   |
		| 219                   | 320   |
	And the Published Provider has the following provider information
		| Field                         | Value                    |
		| ProviderId                    | 1000000                  |
		| Name                          | Maintained School 1      |
		| Authority                     | Local Authority 1        |
		| DateOpened                    | 2012-03-15               |
		| LACode                        | 200                      |
		| LocalAuthorityName            | Maintained School 1      |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000000                  |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 1000000                  |
		| Name                          | Maintained School 1      |
		| Authority                     | Local Authority 1        |
		| DateOpened                    | 2012-03-15               |
		| LACode                        | 200                      |
		| LocalAuthorityName            | Maintained School 1      |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000000                  |
	And the provider with id '1000000' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000002           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 12000             |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name                                                 | FundingLineCode | Value | TemplateLineId | Type        |
		| Pupil Led Factors                                    | 1619-004         | 640   | 5              | Information |
		| Funding Through Premises and Mobility Factors        | 1619-007         | 0     | 6              | Information |
		| Growth funding                                       | 1619-006         | 0     | 7              | Information |
		| CSSB Pupil Led Funding                               | 1619-012         | 320   | 23             | Information |
		| Funding for Historic Commitments                     | 1619-013         | 0     | 27             | Information |
		| Universal Entitlement for 3 and 4 Year Olds          | 1619-014         | 320   | 130            | Information |
		| Funding for Additional Hours for working parents     | 1619-015         | 320   | 135            | Information |
		| Participation Funding for Disadvantaged 2 Year Olds  | 1619-016         | 320   | 140            | Information |
		| Funding Allocation for Early Years Pupil Premium     | 1619-017         | 320   | 145            | Information |
		| Funding Allocation for Maintained Nursery Supplement | 1619-018         | 320   | 150            | Information |
		| Funding Allocation for Disability Access Fund        | 1619-019         | 0     | 155            | Information |
		| Schools Block before recoupment                      | 1619-003         | 640   | 4              | Information |
		| Central School Services Block                        | 1619-008         | 320   | 8              | Information |
		| Early Years Block                                    | 1619-009         | 1600  | 9              | Information |
		| High Needs Block before deductions                   | 1619-010         | 1280  | 10             | Information |
		| Total High Needs Block After Deductions              | 1619-011         | 14720 | 11             | Information |
		| School Block After recoupment                        | 1619-005         | 640   | 12             | Information |
		| Provisional Schools Block Funding Excluding Growth   | 1619-020         | 0     | 239            | Information |
		| LA Protection                                        | 1619-021         | 0     | 240            | Information |
		| Total 1619 before deductions and recoupment           | 1619-001         | 3840  | 2              | Information |
		| Total 1619 after deductions and recoupment            | 1619-002         | 15360 | 3              | Payment     |
	And the Published Provider has the following distribution period for funding line '1619-002'
		| DistributionPeriodId | Value |
		| AS-1920              | 7000  |
		| AS-2021              | 5000  |
	And the Published Providers distribution period has the following profiles for funding line '1619-002'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AS-1920              | CalendarMonth | October   | 1920 | 1          | 7000          |
		| AS-2021              | CalendarMonth | April     | 2021 | 1          | 5000          |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 238                   | 320   |
		| 15                    | 320   |
		| 16                    | 320   |
		| 17                    | 320   |
		| 18                    | 320   |
		| 13                    | 320   |
		| 14                    | 320   |
		| 19                    | 320   |
		| 20                    | 320   |
		| 25                    | 320   |
		| 26                    | 320   |
		| 24                    | 320   |
		| 28                    | 320   |
		| 132                   | 320   |
		| 133                   | 320   |
		| 134                   | 320   |
		| 131                   | 320   |
		| 137                   | 320   |
		| 138                   | 320   |
		| 139                   | 320   |
		| 136                   | 320   |
		| 142                   | 320   |
		| 143                   | 320   |
		| 144                   | 320   |
		| 141                   | 320   |
		| 147                   | 320   |
		| 148                   | 320   |
		| 149                   | 320   |
		| 146                   | 320   |
		| 152                   | 320   |
		| 153                   | 320   |
		| 154                   | 320   |
		| 151                   | 320   |
		| 31                    | 320   |
		| 32                    | 320   |
		| 34                    | 320   |
		| 35                    | 320   |
		| 37                    | 320   |
		| 38                    | 320   |
		| 29                    | 320   |
		| 237                   | 320   |
		| 30                    | 320   |
		| 33                    | 320   |
		| 36                    | 320   |
		| 65                    | 320   |
		| 66                    | 320   |
		| 67                    | 320   |
		| 68                    | 320   |
		| 64                    | 320   |
		| 70                    | 320   |
		| 71                    | 320   |
		| 235                   | 320   |
		| 236                   | 320   |
		| 241                   | 320   |
		| 242                   | 320   |
		| 243                   | 320   |
		| 244                   | 320   |
		| 245                   | 320   |
		| 246                   | 320   |
		| 247                   | 320   |
		| 248                   | 320   |
		| 249                   | 320   |
		| 250                   | 320   |
		| 72                    | 320   |
		| 73                    | 320   |
		| 69                    | 320   |
		| 75                    | 320   |
		| 76                    | 320   |
		| 77                    | 320   |
		| 78                    | 320   |
		| 74                    | 320   |
		| 80                    | 320   |
		| 81                    | 320   |
		| 82                    | 320   |
		| 83                    | 320   |
		| 79                    | 320   |
		| 50                    | 320   |
		| 51                    | 320   |
		| 52                    | 320   |
		| 53                    | 320   |
		| 163                   | 320   |
		| 164                   | 320   |
		| 165                   | 320   |
		| 166                   | 320   |
		| 162                   | 320   |
		| 90                    | 320   |
		| 91                    | 320   |
		| 92                    | 320   |
		| 93                    | 320   |
		| 89                    | 320   |
		| 95                    | 320   |
		| 96                    | 320   |
		| 97                    | 320   |
		| 98                    | 320   |
		| 94                    | 320   |
		| 158                   | 320   |
		| 175                   | 320   |
		| 174                   | 320   |
		| 168                   | 320   |
		| 169                   | 320   |
		| 170                   | 320   |
		| 171                   | 320   |
		| 167                   | 320   |
		| 176                   | 320   |
		| 177                   | 320   |
		| 178                   | 320   |
		| 179                   | 320   |
		| 232                   | 320   |
		| 182                   | 320   |
		| 183                   | 320   |
		| 180                   | 320   |
		| 181                   | 320   |
		| 234                   | 320   |
		| 159                   | 320   |
		| 161                   | 320   |
		| 233                   | 320   |
		| 189                   | 320   |
		| 190                   | 320   |
		| 187                   | 320   |
		| 188                   | 320   |
		| 186                   | 320   |
		| 195                   | 320   |
		| 196                   | 320   |
		| 193                   | 320   |
		| 194                   | 320   |
		| 192                   | 320   |
		| 185                   | 320   |
		| 191                   | 320   |
		| 202                   | 320   |
		| 203                   | 320   |
		| 200                   | 320   |
		| 201                   | 320   |
		| 199                   | 320   |
		| 198                   | 320   |
		| 209                   | 320   |
		| 210                   | 320   |
		| 207                   | 320   |
		| 208                   | 320   |
		| 206                   | 320   |
		| 205                   | 320   |
		| 214                   | 320   |
		| 215                   | 320   |
		| 213                   | 320   |
		| 223                   | 320   |
		| 224                   | 320   |
		| 221                   | 320   |
		| 222                   | 320   |
		| 220                   | 320   |
		| 212                   | 320   |
		| 128                   | 320   |
		| 129                   | 320   |
		| 228                   | 320   |
		| 229                   | 320   |
		| 227                   | 320   |
		| 226                   | 320   |
		| 42                    | 320   |
		| 156                   | 320   |
		| 157                   | 320   |
		| 184                   | 320   |
		| 197                   | 320   |
		| 204                   | 320   |
		| 211                   | 320   |
		| 218                   | 320   |
		| 225                   | 320   |
		| 40                    | 320   |
		| 41                    | 320   |
		| 39                    | 320   |
		| 21                    | 320   |
		| 22                    | 320   |
		| 216                   | 320   |
		| 217                   | 320   |
		| 219                   | 320   |
	And the Published Provider has the following provider information
		| Field                         | Value                    |
		| ProviderId                    | 1000002                  |
		| Name                          | Maintained School 2      |
		| Authority                     | Local Authority 1        |
		| DateOpened                    | 2012-03-15               |
		| LACode                        | 200                      |
		| LocalAuthorityName            | Maintained School 2      |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000002                  |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                    |
		| ProviderId                    | 1000002                  |
		| Name                          | Maintained School 2      |
		| Authority                     | Local Authority 1        |
		| DateOpened                    | 2012-03-15               |
		| LocalAuthorityName            | Maintained School 2      |
		| LACode                        | 200                      |
		| ProviderType                  | LA maintained schools    |
		| ProviderSubType               | Community school         |
		| ProviderVersionId             | <ProviderVersionId>      |
		| TrustStatus                   | Not Supported By A Trust |
		| UKPRN                         | 1000002                  |
		
	And the provider with id '1000002' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	# Local Authorities in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
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
	And calculations exists
		| Value | Id                                   |
		| 320   | e46c0b59-6900-4fae-90b5-da6e558d9491 |
		| 320   | dd54d574-f9ef-4bef-bf82-1cb31c526f36 |
		| 320   | ff97843a-5fa9-4435-81c3-c44785611b01 |
		| 320   | b7c8bac7-c470-4b9b-af8f-0e4bd9886742 |
		| 320   | f43ba392-bf58-4677-a2c4-87ac114498fe |
		| 320   | 54dea110-7fb7-41c0-bf87-2050ad54a042 |
		| 320   | c71aa30e-73a4-4b5e-b1f3-61856b92b1fd |
		| 320   | 21c65d92-31a8-4b08-9a78-caada398b8d2 |
		| 320   | a130170a-b70e-463c-960d-639e2155ca3e |
		| 320   | fcd312aa-c9bb-4b1f-97b2-2efeaccc6c5a |
		| 320   | bd784ee0-e837-4859-8891-1f991d429657 |
		| 320   | e79438f4-be66-4653-b61b-a2f7233c4549 |
		| 320   | 7709b5c0-699f-455e-8773-7b258d801792 |
		| 320   | 6d1c482b-6df9-4ba4-842e-704aa5f6100f |
		| 320   | 771727d2-61a7-4928-b9c5-71e80ace0c4d |
		| 320   | f5afea39-d21c-443b-859a-595c78789989 |
		| 320   | 36df4068-f3e0-4040-a73f-40dc66e1bad0 |
		| 320   | 989fe1d5-9913-49d8-ba07-5685052e818f |
		| 320   | 704db23d-cf70-4a14-93b4-c966727cedb0 |
		| 320   | df645907-2cf0-4f25-99a4-4147ac62e1df |
		| 320   | 300dbe4d-b008-4ede-a189-4b456fac6870 |
		| 320   | 96d3e984-ccf5-440e-960a-fcbea5034e74 |
		| 320   | ddfdaf6f-0d81-4655-b71e-2c8b9320329c |
		| 320   | 030997f5-38c6-453e-a40a-5f640d2bf223 |
		| 320   | e616e94c-bdf9-4746-aba9-cbe3ee7b27cf |
		| 320   | ccacff9b-1767-4045-82ff-669d56c018ca |
		| 320   | 18274db3-2c2d-4c2d-a46c-17cc5d98737d |
		| 320   | 644667cc-c636-47b9-ade8-e2f7af501388 |
		| 320   | c14a47a0-3a88-4dd9-9bd1-a5935e0cb49b |
		| 320   | 4a4a6b45-a540-4efa-be10-8188d04460fe |
		| 320   | a53c47a9-b181-4158-902c-abe74efeccfa |
		| 320   | 67e747b8-1d25-4de0-8101-7f81e8d7c051 |
		| 320   | b7ec40dc-1f24-4b69-b0dc-ecc4a7f9eb5c |
		| 320   | b64dbf4f-5ac4-4ad3-91cd-c98ce40c606a |
		| 320   | d3704649-3119-42e1-ab69-b1758f2c0fd5 |
		| 320   | 3647fb00-31d9-4c3a-a20a-e8631810dd55 |
		| 320   | 49494307-c1a1-40a5-a2d5-38d88bca8ede |
		| 320   | 5f19e35b-f532-43e7-9133-638a29324815 |
		| 320   | 9c0f0435-5268-49c0-a0a7-74f8d477a52a |
		| 320   | fae774f5-f73c-4bd1-8fc8-aee66fb51c0c |
		| 320   | 61709649-863e-4a1f-b03b-ddcac25625a4 |
		| 320   | b9dd905a-8aa4-41ee-8f90-f0d655f3553f |
		| 320   | 9756e012-9f5e-47c9-a5b8-c1c7188b6829 |
		| 320   | d95c681b-66f4-49e5-bf6e-22f330afe3b4 |
		| 320   | 0b643470-b859-4a5e-b2cd-f45a33657466 |
		| 320   | 4c0eaa57-642e-4fba-9c75-4174c4be0ab4 |
		| 320   | 4543e33f-8b40-4335-901d-834ab84b761f |
		| 320   | 2375efb5-4275-4f64-96bf-01c35b4dfc54 |
		| 320   | 07400efc-cd8a-4062-bdde-3dac44bce704 |
		| 320   | b4290464-36ed-42ca-9df9-822003200b9d |
		| 320   | 8f967c9a-40e2-4842-8c55-969abf5a350f |
		| 320   | 6bb71c56-818a-4ab1-b35f-ee497378ae3d |
		| 320   | 42919210-5ab6-4f98-8cbd-36ea585bfc3e |
		| 320   | 50d71379-e1e6-4f9a-8a82-8d8697049662 |
		| 320   | ac233126-aa00-470a-a2db-aed95c7bead5 |
		| 320   | b7fd2f32-1e43-4b3b-84f9-9754f71a0E9f |
		| 320   | b5f7f814-e819-4efb-9c07-e6b973a5dc30 |
		| 320   | c36e9b84-8c83-45e6-8ccf-1d06c83d96d1 |
		| 320   | 1f72e2d2-43fc-45df-b3e2-03a1d5E7d6d7 |
		| 320   | b2fc456d-7191-4214-9db5-1561cd3f75fa |
		| 320   | f8b65554-b578-4041-a9F6-116830cc0a2b |
		| 320   | 1d05db55-d225-49a3-9240-fce6e9065de5 |
		| 320   | bd4b64d4-f61f-4207-a493-73cb8d1e0db8 |
		| 320   | bd3ef104-827f-42a0-a1c0-365bc5c21156 |
		| 320   | 2794481e-5514-49fd-bcb7-0a91b0d40647 |
		| 320   | 16225034-ba94-4ecd-bb26-703c1ed4df29 |
		| 320   | a9072025-e28a-4302-94d7-46e5a48acf38 |
		| 320   | 01b34a35-4929-4896-8a25-6e9ccdf38607 |
		| 320   | e7c1cedf-254f-4635-8f71-e2c6688eb7f0 |
		| 320   | d7219481-eb38-48f4-a6c2-5775b6d8833e |
		| 320   | fdc6128c-4dca-456d-807d-252e93a2bdca |
		| 320   | 7b9dd9be-e63c-4223-ba2d-2a7593e4b3ee |
		| 320   | d53b992a-5484-4d9f-91d7-aa4cf297e9b5 |
		| 320   | 766fd82c-bea2-476f-805f-ede2d6a662f0 |
		| 320   | 1972f183-a4ea-4f91-b064-1a2f9d11eb71 |
		| 320   | b8601c5c-f3eb-45d3-abfe-84120b8ff371 |
		| 320   | 47b47407-00c2-4888-9c3d-cb22b6e62384 |
		| 320   | 3ba563a7-4611-4c2e-9951-1595969bf9d2 |
		| 320   | ca896c28-5b68-4f36-a699-42da4640a36d |
		| 320   | f68fa739-4eea-4ba3-9e49-5e96d23731bc |
		| 320   | 33b8ea92-2344-4429-b28a-1cff65745e6f |
		| 320   | 108caf20-de82-49e1-8779-48f0326807dc |
		| 320   | ddb3eb07-917a-4080-b338-98e985c21ec8 |
		| 320   | f89336ae-1ae7-44b1-8b21-306697c8a2ad |
		| 320   | f9c77603-cc44-497c-8ba4-3d41dbd14c26 |
		| 320   | f2b7cf2a-495d-4157-ac97-789f86a9073a |
		| 320   | 9491613b-b7a2-42ef-b590-51dd003d55c4 |
		| 320   | d896cdd6-4b3f-4136-81e5-e892b8468b02 |
		| 320   | 49ac5375-2c3f-4d5e-88f9-6f35be9e251b |
		| 320   | f8bdcb90-cfdc-424f-bfab-6e829cfe9140 |
		| 320   | 746b26a3-9077-4bc5-8b3e-6a562ccb66de |
		| 320   | d0e4f8b7-1fba-4edd-9ed6-30c92e509584 |
		| 320   | a4ec12da-02ba-4103-816d-296e7156c5b0 |
		| 320   | d758185f-b7b3-4ddf-a63f-3f68c1ceb18e |
		| 320   | da3f8c8b-9d51-4ac2-89bf-ce3875e6c073 |
		| 320   | 3973e082-7248-44c0-89b1-7a1939927ab6 |
		| 320   | 1d818411-fb19-4589-b541-d3fb3b941d52 |
		| 320   | 161c81cc-1412-4c9d-aecf-0688f6075323 |
		| 320   | deddfe8a-6f21-405d-8f8e-b3d77004785b |
		| 320   | c93608d3-58d7-4a6d-89ec-ba2c1fb77fb6 |
		| 320   | 85d6df27-22b5-4172-ac63-c03edb954ea6 |
		| 320   | a0320c8b-9dae-4719-9708-70076cb240f1 |
		| 320   | c437b189-b105-469f-b3a0-2e2ccbddc989 |
		| 320   | 89167b75-49b1-4d2a-b3ee-70bae9242317 |
		| 320   | 0be04e61-d828-46df-b903-20f1e7d49a0d |
		| 320   | 0ade4f3f-8ce0-4a1a-8499-52272d673b74 |
		| 320   | 9c2a3458-9c47-4634-8b01-c61aef671c65 |
		| 320   | fe1d36c8-4d60-45e0-afd9-25e81d4de265 |
		| 320   | 54ae55fe-5c09-4393-a9ef-83a13d45439e |
		| 320   | 6e8bb519-2f0a-4e43-98c6-cb8a4b0e438c |
		| 320   | 870b5a27-4217-4b6e-bce3-9e5c823adbe1 |
		| 320   | f55b3de5-55e6-443e-8aa3-0e14f1f38eda |
		| 320   | 4d661833-9e57-4535-81fc-6de279af494f |
		| 320   | e92d3c27-3d11-45ce-aaf5-dbbdd9f672c5 |
		| 320   | 8dbc440e-5cef-45ec-ba6c-c7625f118136 |
		| 320   | 95eb6ce5-caad-478d-bbc6-2e913e4dd727 |
		| 320   | c3962fe9-9545-427e-ae69-0719c87971eb |
		| 320   | 841dd7e3-c39a-40eb-a80e-b380be8b5459 |
		| 320   | 17ba6093-fc27-4d08-8e41-981e1a910bda |
		| 320   | aabee9ce-4e36-461d-b166-b8b794bac007 |
		| 320   | b700cbcd-5594-4bac-ae2e-07a179f40830 |
		| 320   | 1993ff13-6371-4d78-8e23-cf8e37f051db |
		| 320   | 83e7f9cb-fff2-43b7-a81e-c8af459d4e54 |
		| 320   | ea07237f-3f20-4590-9184-2f9426a31c0a |
		| 320   | 0ca7fdb5-b1ba-4cb3-80a9-c71f541c3802 |
		| 320   | bf3a8703-e7f9-4f91-8466-b7e367cf6a3e |
		| 320   | 7226a3f2-e085-45d4-bfad-3d424d81644f |
		| 320   | 778ab5d8-c374-49b1-893e-9542e4b2c682 |
		| 320   | 008e0d05-2aa1-4ea8-b609-c929654bf864 |
		| 320   | 67d4c440-649c-4c41-ad02-42b0f2d2d2f3 |
		| 320   | 07e3104b-43b4-43cb-9cd3-15a696847603 |
		| 320   | 1edc3242-5e80-4e2e-808f-eb73545daaee |
		| 320   | 420dd58c-cf5e-4d36-9dd2-8f3118011333 |
		| 320   | 8389a997-4145-43e5-94d6-d1cb4c72e6ef |
		| 320   | df34bbaf-09ca-49df-8b64-d49d90bd1523 |
		| 320   | 9567eecd-f7a7-48b5-b11e-38877ccc6603 |
		| 320   | 57d5a373-0f6e-43d0-9164-d780316fef9a |
		| 320   | ee2011d2-864d-47d3-b19f-fadd4f9d21be |
		| 320   | 8618821c-e21f-47e5-a97d-65973459fbab |
		| 320   | 7556232a-1ad7-42ad-b766-a27ce3dba4e2 |
		| 320   | a7e1682f-8004-4bc8-a720-2f564416a725 |
		| 320   | e47315c0-2114-4756-9ee8-0d0ec44c5771 |
		| 320   | 0da63f9a-d7cd-4cf2-8db3-0b5a678040ab |
		| 320   | 01e00072-a670-4bee-b0c8-53454460027e |
		| 320   | 4a47a382-cb38-4bcc-9b22-897c092084c7 |
		| 320   | f41f06bc-6717-4791-bfc7-aa4fe8981b7d |
		| 320   | e539ecee-8ae6-4ce1-9e1f-453a888940f0 |
		| 320   | a44ba971-8d73-4d65-85ff-8f04aa963f3d |
		| 320   | ca6d0788-8c26-4e02-be57-52b47ef72267 |
		| 320   | c1faf3ac-759e-4455-8b01-bef377274e08 |
		| 320   | 8e2364b5-258f-4955-88ea-a5db1c958cad |
		| 320   | a78dac8f-3ccf-42af-a137-719202a52730 |
		| 320   | 1b91566c-ccab-42b5-84fc-b71b47c9e7bc |
		| 320   | 2c245fa9-a9d6-46e6-a03a-41e2123f40d1 |
		| 320   | 413ffcef-bb79-41a6-8c9a-bd11c7db9f70 |
		| 320   | 372b42ed-5e9e-4e7b-99be-6c14b9def29b |
		| 320   | 28ffe367-6171-43a5-830c-24925ff63d06 |
		| 320   | d04fbcf8-4221-462c-b045-9e124b3775c2 |
		| 320   | 18376b64-620a-4c34-ad44-6579013a58e6 |
		| 320   | 11f6832f-5abb-45cb-9afe-539788754a72 |
		| 320   | 780d80b5-38e3-421f-90fc-9f4a351df76f |
		| 320   | c4f37785-819b-4680-9427-ce8f887285f4 |
		| 320   | dfe0db99-6652-46fd-8a90-6efe74642f55 |
		| 320   | 683d948a-1aa5-4e90-b5fc-205d040772b8 |
		| 320   | 58a67753-b104-455d-ae5d-fd303fe5b18d |
		| 320   | b03fbd41-8454-4a4e-a9d1-e6cdfd4eeb60 |
		| 320   | 8277b567-6d82-4760-95d3-56e26d31704e |
		| 320   | 82a82df7-a4e3-4d5d-b378-23af3607ceec |
		| 320   | 1c066b09-3e98-48fc-966f-ea3f09401b54 |
		| 320   | 4afd7426-1787-4b71-a5f7-971db86811be |
		| 320   | 8fdd4341-88ea-47f2-ba81-511951ca7efd |
		| 320   | 5cfb28de-88d6-4faa-a936-d81a065fb596 |
	When funding is published
	Then the following published funding is produced
		| Field                            | Value             |
		| GroupingReason                   | Payment           |
		| OrganisationGroupTypeCode        | LocalAuthority    |
		| OrganisationGroupIdentifierValue | 9000000           |
		| FundingPeriodId                  | <FundingPeriodId> |
		| FundingStreamId                  | <FundingStreamId> |
	And the total funding is '24000'
	And the published funding contains the following published provider ids
		| FundingIds                                      |
		| <FundingStreamId>-<FundingPeriodId>-1000000-1_0 |
		| <FundingStreamId>-<FundingPeriodId>-1000002-1_0 |
	And the published funding contains a distribution period in funding line '1619-002' with id of 'AS-1920' has the value of '14000'
	And the published funding contains a distribution period in funding line '1619-002' with id of 'AS-2021' has the value of '10000'
	And the published funding contains a distribution period in funding line '1619-002' with id of 'AS-1920' has the following profiles
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AS-1920              | CalendarMonth | October   | 1920 | 1          | 14000         |
	And the published funding contains a distribution period in funding line '1619-002' with id of 'AS-2021' has the following profiles
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AS-2021              | CalendarMonth | April     | 2021 | 1          | 10000         |
	And  the published funding contains a calculations in published provider with following calculation results
		| Id				    | Value |		
		| 238                   | 640   |
		| 15                    | 640   |
		| 17                    | 640   |
		| 13                    | 640   |
		| 14                    | 640   |
		| 19                    | 640   |
		| 20                    | 640   |
		| 26                    | 640   |
		| 24                    | 640   |
		| 28                    | 640   |
		| 133                   | 320   |
		| 131                   | 640   |
		| 138                   | 320   |
		| 136                   | 640   |
		| 143                   | 640   |
		| 141                   | 640   |
		| 148                   | 640   |
		| 146                   | 640   |
		| 153                   | 640   |
		| 151                   | 640   |
		| 32                    | 640   |
		| 34                    | 640   |
		| 37                    | 640   |
		| 29                    | 640   |
		| 237                   | 640   |
		| 30                    | 640   |
		| 33                    | 640   |
		| 36                    | 320   |
		| 65                    | 640   |
		| 66                    | 640   |
		| 64                    | 640   |
		| 70                    | 640   |
		| 71                    | 640   |
		| 69                    | 640   |
		| 75                    | 640   |
		| 76                    | 640   |
		| 74                    | 640   |
		| 80                    | 640   |
		| 81                    | 640   |
		| 79                    | 640   |
		| 50                    | 640   |
		| 51                    | 640   |
		| 52                    | 640   |
		| 53                    | 640   |
		| 163                   | 640   |
		| 164                   | 640   |
		| 162                   | 640   |
		| 90                    | 640   |
		| 91                    | 640   |
		| 89                    | 640   |
		| 95                    | 640   |
		| 96                    | 640   |
		| 94                    | 640   |
		| 158                   | 640   |
		| 175                   | 640   |
		| 174                   | 640   |
		| 168                   | 640   |
		| 169                   | 640   |
		| 167                   | 640   |
		| 176                   | 640   |
		| 177                   | 640   |
		| 232                   | 640   |
		| 182                   | 640   |
		| 183                   | 640   |
		| 234                   | 640   |
		| 159                   | 640   |
		| 161                   | 640   |
		| 233                   | 640   |
		| 189                   | 640   |
		| 190                   | 640   |
		| 186                   | 640   |
		| 195                   | 640   |
		| 196                   | 640   |
		| 192                   | 640   |
		| 185                   | 640   |
		| 191                   | 640   |
		| 202                   | 640   |
		| 203                   | 640   |
		| 199                   | 640   |
		| 198                   | 640   |
		| 209                   | 640   |
		| 210                   | 640   |
		| 206                   | 640   |
		| 205                   | 640   |
		| 214                   | 640   |
		| 215                   | 640   |
		| 213                   | 640   |
		| 223                   | 640   |
		| 224                   | 640   |
		| 220                   | 640   |
		| 212                   | 640   |
		| 128                   | 640   |
		| 129                   | 640   |
		| 227                   | 640   |
		| 226                   | 640   |
		| 42                    | 640   |
		| 156                   | 640   |
		| 157                   | 640   |
		| 184                   | 640   |
		| 197                   | 640   |
		| 204                   | 640   |
		| 211                   | 640   |
		| 218                   | 640   |
		| 225                   | 640   |
		| 40                    | 640   |
		| 41                    | 640   |
		| 39                    | 640   |
		| 21                    | 640   |
		| 22                    | 640   |
		| 219                   | 640   |
	And the published funding document produced is saved to blob storage for following file name
		| PublishedFundingFiles												   |
		| <FundingStreamId>-<FundingPeriodId>-Information-LocalAuthority-200-1_0.json |
		| <FundingStreamId>-<FundingPeriodId>-Payment-LocalAuthority-9000000-1_0.json |
	And the published funding document produced has following metadata
		| PublishedFundingFiles												   | MetadataKey | MetadataValue | 
		| <FundingStreamId>-<FundingPeriodId>-Information-LocalAuthority-200-1_0.json | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-Payment-LocalAuthority-9000000-1_0.json | specification-id | specForPublishing |
	And the published provider document produced is saved to blob storage for following file name
		| PublishedProviderFiles							   |
		| <FundingStreamId>-<FundingPeriodId>-1000000-1_0.json |
		| <FundingStreamId>-<FundingPeriodId>-1000002-1_0.json |
	And the published provider document produced has following metadata
		| PublishedFundingFiles												   | MetadataKey | MetadataValue | 
		| <FundingStreamId>-<FundingPeriodId>-1000000-1_0.json | specification-id | specForPublishing |
		| <FundingStreamId>-<FundingPeriodId>-1000002-1_0.json | specification-id | specForPublishing |
	And the following published provider search index items is produced for providerid with '<FundingStreamId>' and '<FundingPeriodId>'
		| ID                  | ProviderType				| ProviderSubType | LocalAuthority		| FundingStatus | ProviderName			| UKPRN		 | FundingValue | SpecificationId   | FundingStreamId   | FundingPeriodId   | Errors |
		| 1619-AS-2021-1000000 | LA maintained schools		|Community school	|  Local Authority 1 |  Released      | Maintained School 1	| 1000000    | 12000        | specForPublishing | <FundingStreamId> | <FundingPeriodId> | |
		| 1619-AS-2021-1000002 | LA maintained schools		|Community school  | Local Authority 1	| Released      | Maintained School 2	| 1000002    | 12000        | specForPublishing | <FundingStreamId> | <FundingPeriodId> | |
	And the following job is requested is completed for the current specification
		| Field                  | Value             |
		| JobDefinitionId        | PublishFundingJob |
		| InvokerUserId          | PublishUserId     |
		| InvokerUserDisplayName | Invoker User      |
		| ParentJobId            |                   |
	And the following released published provider ids are upserted
		| PublishedProviderId                                           | Status  |
		| publishedprovider-1000000-<FundingPeriodId>-<FundingStreamId> | Released|	
		| publishedprovider-1000002-<FundingPeriodId>-<FundingStreamId> | Released|
	And the provider variation reasons were recorded
		| ProviderId | VariationReason  |
		| 1000000    | FundingUpdated   |
		| 1000000    | ProfilingUpdated |
		| 1000002    | FundingUpdated   |
		| 1000002    | ProfilingUpdated |

	Examples:
		| FundingStreamId | FundingPeriodId | FundingPeriodName      | TemplateVersion | ProviderVersionId |
		| 1619             | AS-2021         | Financial Year 2020-21 | 1.0             | 1619-providers-1.0 |