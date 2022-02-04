Feature: RefreshGAG
	In order to refresh funding for GAG
	As a funding approver
	I want to refresh funding for all approved providers within a specification

@refresh
Scenario Outline: Successful refresh of funding
	Given a funding configuration exists for funding stream '<FundingStreamId>' in funding period '<FundingPeriodId>'
		| Field                  | Value |
		| DefaultTemplateVersion | 1.2   |
		| PaymentOrganisationSource | PaymentOrganisationFields |
	And the funding configuration has the following organisation group and provider status list 'Open;Open, but proposed to close'
		| Field                     | Value        |
		| GroupTypeIdentifier       | UKPRN        |
		| GroupingReason            | Payment      |
		| GroupTypeClassification   | LegalEntity  |
		| OrganisationGroupTypeCode | AcademyTrust |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group and provider status list 'Open;Open, but proposed to close'
		| Field                     | Value        |
		| GroupTypeIdentifier       | UKPRN        |
		| GroupingReason            | Information  |
		| GroupTypeClassification   | LegalEntity  |
		| OrganisationGroupTypeCode | AcademyTrust |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group and provider status list 'Proposed to open'
		| Field                     | Value        |
		| GroupTypeIdentifier       | UKPRN        |
		| GroupingReason            | Indicative   |
		| GroupTypeClassification   | LegalEntity  |
		| OrganisationGroupTypeCode | AcademyTrust |
	And the funding configuration is available in the policies repository
	And the funding period exists in the policies service
		| Field     | Value               |
		| Id        | <FundingPeriodId>   |
		| Name      | <FundingPeriodName> |
		| StartDate | 2019-08-01 00:00:00 |
		| EndDate   | 2020-07-31 00:00:00 |
		| Period    | 2021                |
		| Type      | FY                  |
	And the following specification exists
		| Field                | Value                             |
		| Id                   | specForPublishing                 |
		| Name                 | Test Specification for Publishing |
		| IsSelectedForFunding | true                              |
		| ProviderVersionId    | <ProviderVersionId>               |
	And the specification has the funding period with id '<FundingPeriodId>' and name '<FundingPeriodName>'
	And the specification has the following funding streams
		| Name | Id                |
		| GAG  | <FundingStreamId> |
	And the specification has the following template versions for funding streams
		| Key               | Value |
		| <FundingStreamId> | 1.2   |
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
		| ProviderVersionId | <ProviderVersionId>  |
		| VersionType       | Custom               |
		| Name              | GAG Provider Version |
		| Description       | Acceptance Tests     |
		| Version           | 1                    |
		| TargetDate        | 2019-12-12 00:00     |
		| FundingStream     | <FundingStreamId>    |
		| Created           | 2019-12-11 00:00     |
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
	# Maintained schools - PublishedProviders
	And the Published Provider has the following funding lines
		| Name                                                                                            | FundingLineCode | Value | TemplateLineId | Type        |
		| SBS Exceptional Factors                                                                         | GAG-004         | 640   | 49             | Payment     |
		| Post Opening Grant - Leadership Diseconomies                                                    | GAG-007         | 0     | 303            | Payment     |
		| Post Opening Grant - Per Pupil Resources                                                        | GAG-006         | 0     | 302            | Payment     |
		| Allocation protection                                                                           | GAG-012         | 320   | 300            | Payment     |
		| De-Delegated funding retained by the LA                                                         | GAG-014         | 320   | 705            | Payment     |
		| SBS Other Factors                                                                               | GAG-003         | 640   | 40             | Payment     |
		| Start up Grant Part A                                                                           | GAG-008         | 320   | 304            | Payment     |
		| Start up Grant Part B                                                                           | GAG-009         | 1600  | 305            | Payment     |
		| Hospital Provision                                                                              | GAG-010         | 1280  | 306            | Payment     |
		| Pre-16 High Needs funding                                                                       | GAG-011         | 14720 | 307            | Payment     |
		| Minimum Funding Guarantee                                                                       | GAG-005         | 640   | 297            | Payment     |
		| SBS Pupil Led Factors                                                                           | GAG-001         | 3840  | 2              | Payment     |
		| PFI Front Loaded                                                                                | GAG-002         | 15360 | 39             | Payment     |
		| School Allocation Block With Notional SEN And DeDelegation                                      |                 | 0     | 0              | Information |
		| School Budget Share                                                                             |                 | 0     | 1              | Information |
		| Primary IDACI Band C Funding                                                                    |                 | 0     | 10             | Information |
		| Primary IDACI Band D Funding                                                                    |                 | 0     | 11             | Information |
		| Primary IDACI Band E Funding                                                                    |                 | 0     | 12             | Information |
		| Primary IDACI Band F Funding                                                                    |                 | 0     | 13             | Information |
		| Secondary IDACI Band A Funding                                                                  |                 | 0     | 14             | Information |
		| Secondary IDACI Band B Funding                                                                  |                 | 0     | 15             | Information |
		| Secondary IDACI Band C Funding                                                                  |                 | 0     | 16             | Information |
		| Secondary IDACI Band D Funding                                                                  |                 | 0     | 17             | Information |
		| Secondary IDACI Band E Funding                                                                  |                 | 0     | 18             | Information |
		| Secondary IDACI Band F Funding                                                                  |                 | 0     | 19             | Information |
		| Primary Free School Meals FSM Funding                                                           |                 | 0     | 20             | Information |
		| Primary FSM6 Funding                                                                            |                 | 0     | 21             | Information |
		| Secondary Free School Meals FSM Funding                                                         |                 | 0     | 22             | Information |
		| Secondary FSM6 Funding                                                                          |                 | 0     | 23             | Information |
		| Looked After Children LA C Funding                                                              |                 | 0     | 24             | Information |
		| School Allocation Block                                                                         |                 | 0     | 242            | Information |
		| Notional SEN Funding                                                                            |                 | 0     | 243            | Information |
		| Basic Entitlement Age Weighted Pupil SEN                                                        |                 | 0     | 244            | Information |
		| Basic Entitlement Primary Including Reception SEN                                               |                 | 0     | 245            | Information |
		| Basic Entitlement KS3 SEN                                                                       |                 | 0     | 246            | Information |
		| Basic Entitlement KS4 SEN                                                                       |                 | 0     | 247            | Information |
		| Deprivation SEN                                                                                 |                 | 0     | 248            | Information |
		| Primary IDACI Band A SEN                                                                        |                 | 0     | 249            | Information |
		| Prior Attainment                                                                                |                 | 0     | 25             | Information |
		| Primary IDACI Band B SEN                                                                        |                 | 0     | 250            | Information |
		| Primary IDACI Band C SEN                                                                        |                 | 0     | 251            | Information |
		| Primary IDACI Band D SEN                                                                        |                 | 0     | 253            | Information |
		| Primary IDACI Band E SEN                                                                        |                 | 0     | 254            | Information |
		| Primary IDACI Band F SEN                                                                        |                 | 0     | 255            | Information |
		| Secondary IDACI Band A SEN                                                                      |                 | 0     | 256            | Information |
		| Secondary IDACI Band B SEN                                                                      |                 | 0     | 257            | Information |
		| Secondary IDACI Band C SEN                                                                      |                 | 0     | 258            | Information |
		| Secondary IDACI Band D SEN                                                                      |                 | 0     | 259            | Information |
		| Primary Attainment Low Primary Prior Attainment Funding                                         |                 | 0     | 26             | Information |
		| Secondary IDACIBand E SEN                                                                       |                 | 0     | 260            | Information |
		| Secondary IDACIBand F SEN                                                                       |                 | 0     | 261            | Information |
		| Primary Free School Meals FSM SEN                                                               |                 | 0     | 262            | Information |
		| Primary FSM6 SEN                                                                                |                 | 0     | 263            | Information |
		| Secondary Free School Meals FSM SEN                                                             |                 | 0     | 264            | Information |
		| Pupil Led Factors SEN                                                                           |                 | 0     | 265            | Information |
		| Other Factors SEN                                                                               |                 | 0     | 266            | Information |
		| Exceptional Factors SEN                                                                         |                 | 0     | 267            | Information |
		| MFG SEN                                                                                         |                 | 0     | 268            | Information |
		| Secondary FSM6 SEN                                                                              |                 | 0     | 269            | Information |
		| Secondary Attainment Secondary Pupils Not Achieving The Expected Standards In KS2 Tests Funding |                 | 0     | 27             | Information |
		| Prior Attainment SEN                                                                            |                 | 0     | 270            | Information |
		| Primary Attainment LowPrimaryPriorAttainmentSEN                                                 |                 | 0     | 271            | Information |
		| Secondary Attainment Secondary Pupils Not Achieving The Expected Standards In KS2 Tests SEN     |                 | 0     | 272            | Information |
		| English As An Additional Language EAL SEN                                                       |                 | 0     | 273            | Information |
		| Primary EAL Band 1 SEN                                                                          |                 | 0     | 274            | Information |
		| Primary EAL Band 2 SEN                                                                          |                 | 0     | 275            | Information |
		| Primary EAL Band 3 SEN                                                                          |                 | 0     | 276            | Information |
		| Secondary EAL Band 1 SEN                                                                        |                 | 0     | 277            | Information |
		| Secondary EAL Band 2 SEN                                                                        |                 | 0     | 278            | Information |
		| Secondary EAL Band 3 SEN                                                                        |                 | 0     | 279            | Information |
		| English As An Additional Language EAL Funding                                                   |                 | 0     | 28             | Information |
		| Mobility SEN                                                                                    |                 | 0     | 280            | Information |
		| Primary Mobility SEN                                                                            |                 | 0     | 281            | Information |
		| Secondary Mobility SEN                                                                          |                 | 0     | 282            | Information |
		| Sparsity SEN                                                                                    |                 | 0     | 283            | Information |
		| Lump Sum SEN                                                                                    |                 | 0     | 284            | Information |
		| Primary Lump Sum SEN                                                                            |                 | 0     | 285            | Information |
		| Secondary Lump Sum SEN                                                                          |                 | 0     | 286            | Information |
		| Split Sites SEN                                                                                 |                 | 0     | 287            | Information |
		| Standard PFI SEN                                                                                |                 | 0     | 288            | Information |
		| MFL SEN                                                                                         |                 | 0     | 289            | Information |
		| Primary EAL Band 1 Funding                                                                      |                 | 0     | 29             | Information |
		| Exceptional Circumstance 1 SEN                                                                  |                 | 0     | 290            | Information |
		| Exceptional Circumstance 2 SEN                                                                  |                 | 0     | 291            | Information |
		| Exceptional Circumstance 3 SEN                                                                  |                 | 0     | 292            | Information |
		| Exceptional Circumstance 4 SEN                                                                  |                 | 0     | 293            | Information |
		| Exceptional Circumstance 5 SEN                                                                  |                 | 0     | 294            | Information |
		| Exceptional Circumstance 6 SEN                                                                  |                 | 0     | 295            | Information |
		| Exceptional Circumstance 7 SEN                                                                  |                 | 0     | 296            | Information |
		| Total Post Opening Grant Start Up Grant Allocation                                              |                 | 0     | 298            | Information |
		| Total High Needs Allocation                                                                     |                 | 0     | 299            | Information |
		| Basic Entitlement Age Weighted Pupil Unit                                                       |                 | 0     | 3              | Information |
		| Primary EAL Band 2 Funding                                                                      |                 | 0     | 30             | Information |
		| Special Unoccupied                                                                              |                 | 0     | 308            | Information |
		| Special Occupied                                                                                |                 | 0     | 309            | Information |
		| Primary EAL Band 3 Funding                                                                      |                 | 0     | 31             | Information |
		| Alternative Provision                                                                           |                 | 0     | 310            | Information |
		| Secondary EAL Band 1 Funding                                                                    |                 | 0     | 32             | Information |
		| Secondary EAL Band 2 Funding                                                                    |                 | 0     | 33             | Information |
		| Looked After Children LAC SEN                                                                   |                 | 0     | 337            | Information |
		| Secondary EALBand3Funding                                                                       |                 | 0     | 34             | Information |
		| Mobility Funding                                                                                |                 | 0     | 35             | Information |
		| Primary Mobility Funding                                                                        |                 | 0     | 36             | Information |
		| Secondary Mobility Funding                                                                      |                 | 0     | 37             | Information |
		| SBS Other Factors Summary                                                                       |                 | 0     | 38             | Information |
		| Basic Entitlement Primary Funding                                                               |                 | 0     | 4              | Information |
		| Sparsity Funding                                                                                |                 | 0     | 41             | Information |
		| Lump Sum                                                                                        |                 | 0     | 42             | Information |
		| Primary Lump Sum                                                                                |                 | 0     | 43             | Information |
		| Secondary Lump Sum                                                                              |                 | 0     | 44             | Information |
		| SplitSite                                                                                       |                 | 0     | 45             | Information |
		| PFI                                                                                             |                 | 0     | 46             | Information |
		| London Fringe                                                                                   |                 | 0     | 47             | Information |
		| MFL Adjustment                                                                                  |                 | 0     | 48             | Information |
		| Basic Entitlement KS3 Funding                                                                   |                 | 0     | 5              | Information |
		| Exceptional Circumstance 1 Funding                                                              |                 | 0     | 50             | Information |
		| Exceptional Circumstance 2 Funding                                                              |                 | 0     | 51             | Information |
		| Exceptional Circumstance 3 Funding                                                              |                 | 0     | 52             | Information |
		| Exceptional Circumstance 4 Funding                                                              |                 | 0     | 53             | Information |
		| Exceptional Circumstance 5 Funding                                                              |                 | 0     | 54             | Information |
		| Exceptional Circumstance 6 Funding                                                              |                 | 0     | 55             | Information |
		| Exceptional Circumstance 7 Funding                                                              |                 | 0     | 56             | Information |
		| Prior Year Adjustment To SBS                                                                    |                 | 0     | 57             | Information |
		| Basic Entitlement KS4 Funding                                                                   |                 | 0     | 6              | Information |
		| Deprivation                                                                                     |                 | 0     | 7              | Information |
		| Funding Previously De Delegated                                                                 |                 | 0     | 704            | Information |
		| Total School Allocation With High Needs                                                         |                 | 0     | 718            | Information |
		| Primary IDACI Band A Funding                                                                    |                 | 0     | 8              | Information |
		| Primary IDACI Band B Funding                                                                    |                 | 0     | 9              | Information |
	And the Published Provider has the following distribution period for funding line 'GAG-002'
		| DistributionPeriodId | Value |
		| AC-1920              | 5000  |
		| AC-2021              | 10360 |
	And the Published Providers distribution period has the following profiles for funding line 'GAG-002'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AC-1920              | CalendarMonth | October   | 1920 | 1          | 5000          |
		| AC-2021              | CalendarMonth | April     | 2021 | 1          | 10360         |
	And template mapping exists
		| EntityType  | CalculationId                        | TemplateId | Name                                                                                                |
		| Calculation | 9ed150c9-072d-4c32-be8a-d0ce83e2dd1a | 703        | Reception Uplift pupil numbers                                                                      |
		| Calculation | 4f6058c6-07e8-4556-8c79-ec703ec55ee7 | 59         | Primary Basic Entitlement Rate                                                                      |
		| Calculation | 6c7c925a-fba0-4950-8aa3-5d1bb567081a | 60         | Primary Basic Entitlement Factor                                                                    |
		| Calculation | eca30f3a-c262-4435-84a8-13e3e1af67bf | 61         | Primary Pupil Count  SBS                                                                            |
		| Calculation | 60b82ba0-785f-4709-8905-020ec4765391 | 238        | Fringe Factor                                                                                       |
		| Calculation | 2e49d940-309f-448c-8e2d-50017ff31db8 | 58         | Basic Entitlement - Primary Funding                                                                 |
		| Calculation | baca161f-899c-4ab2-80eb-23d6b0bbb56c | 314        | Basic Entitlement - Primary SEN %                                                                   |
		| Calculation | 07a0046c-5e37-4521-86b7-dca803cd24f4 | 237        | London Fringe Factor                                                                                |
		| Calculation | f5e5e354-2529-49f3-baf0-d13e4e680097 | 312        | Basic Entitlement - Primary SEN Total                                                               |
		| Calculation | a82e0f02-aaf8-4528-be5b-980ef36c58d5 | 63         | KS3 Basic Entitlement Rate                                                                          |
		| Calculation | 94b9f8e4-bebe-4831-b9bc-43bc80886d5a | 64         | KS3 Basic Entitlement Factor                                                                        |
		| Calculation | 6a575190-07d7-41c7-9e8e-7f23c961e75c | 66         | KS3 Pupil Count  SBS                                                                                |
		| Calculation | 76fe73f1-59c5-400b-b406-1b3a79cea749 | 62         | Basic Entitlement - KS3 Funding                                                                     |
		| Calculation | 16308fe0-8b90-44b4-aa72-ae53914c5d6c | 317        | Basic Entitlement - KS3 SEN %                                                                       |
		| Calculation | 45049cd3-f7ff-4010-8614-5ba4940e4866 | 315        | Basic Entitlement - KS3 SEN Total                                                                   |
		| Calculation | ccf52cc7-11bf-4552-b7e2-9a4f01f7ecea | 68         | KS4 Basic Entitlement Rate                                                                          |
		| Calculation | d4832342-6fe3-4f7c-9d01-4958e35e6033 | 69         | KS4 Basic Entitlement Factor                                                                        |
		| Calculation | 3a99311e-7911-4495-bc3e-b918cf9a2f64 | 70         | KS4 Pupil Count  SBS                                                                                |
		| Calculation | a989532b-79fd-4ec2-a578-b212dcdd349d | 67         | Basic Entitlement - KS4 Funding                                                                     |
		| Calculation | c3bc3133-2db7-4b15-83f3-d4e6c29823b3 | 321        | Basic Entitlement - KS4 SEN %                                                                       |
		| Calculation | 9340b58d-39c9-4f82-bab6-b2dbaebc2037 | 319        | Basic Entitlement - KS4 SEN Total                                                                   |
		| Calculation | 84730c25-6d54-4d83-8a35-35635039a0b3 | 72         | Primary IDACI Band A Rate                                                                           |
		| Calculation | d27a0649-383a-42b0-9e9e-20b2e9b66013 | 73         | Primary IDACI Band A factor                                                                         |
		| Calculation | 249746cc-1a3f-48f0-98a6-1462be3c055f | 71         | Primary IDACI Band A funding                                                                        |
		| Calculation | 4e9e5f06-3e8a-45b5-9a5e-0e84fd6136b8 | 324        | Primary IDACI Band A SEN %                                                                          |
		| Calculation | 46de110c-a53a-40a6-ac3c-25c22933690d | 322        | Primary IDACI Band A SEN Total                                                                      |
		| Calculation | 27ec0c04-218d-430e-bb2a-4f779e5337c8 | 76         | Primary IDACI Band B Rate                                                                           |
		| Calculation | 439ea611-0fd4-4a8a-9795-3466a2d7d87d | 77         | Primary IDACI Band B Factor                                                                         |
		| Calculation | 36568277-f367-4c09-9bc5-d9d445fc0665 | 75         | Primary IDACI Band B funding                                                                        |
		| Calculation | c02e8a8a-a81b-4317-be9b-41cfe2621ba7 | 327        | Primary IDACI Band B SEN %                                                                          |
		| Calculation | 90aa01f2-aa21-4de7-95c5-8d5b65c0e621 | 325        | Primary IDACI Band B SEN Total                                                                      |
		| Calculation | 117103ed-e783-4f67-bd67-f1a15622355f | 80         | Primary IDACI Band C Rate                                                                           |
		| Calculation | 57ecf3c7-c4ab-4e55-b8a3-e6929722ae29 | 81         | Primary IDACI Band C Factor                                                                         |
		| Calculation | 8e8c8a46-89d2-4e31-a606-1b33d7e90c69 | 79         | Primary IDACI Band C funding                                                                        |
		| Calculation | 09917096-d399-484a-af15-82f5375fa6fa | 330        | Primary IDACI Band C SEN %                                                                          |
		| Calculation | 3e0a9c97-c89e-4c12-9fee-3a30b87dc2d5 | 328        | Primary IDACI Band C SEN Total                                                                      |
		| Calculation | 29608864-b47d-461b-9dc5-e6c064d6453c | 340        | Primary IDACI Band D SEN %                                                                          |
		| Calculation | 6e661f39-f7aa-44f3-b57e-4a4b2bfa772f | 338        | Primary IDACI Band D SEN Total                                                                      |
		| Calculation | 7c36427f-b696-4c9b-9646-ace4c7055aaf | 88         | Primary IDACI Band E Rate                                                                           |
		| Calculation | a7308e2c-9e81-44dc-940d-31dcbfea393d | 89         | Primary IDACI Band E Factor                                                                         |
		| Calculation | 1d88ae14-b911-48a9-a25a-78459c199ad9 | 87         | Primary IDACI Band E funding                                                                        |
		| Calculation | 46a69894-a912-4495-a7fd-cac29fe77e1d | 343        | Primary IDACI Band E SEN %                                                                          |
		| Calculation | 69bf74cd-8099-42b7-90a8-ebeecc5151f0 | 341        | Primary IDACI Band E SEN Total                                                                      |
		| Calculation | 04c5b6e5-b5c9-40c4-a04c-eae91b899c86 | 92         | Primary IDACI Band F Rate                                                                           |
		| Calculation | 058f94c5-4080-49fe-ab87-b92ada5bd86d | 93         | Primary IDACI Band F Factor                                                                         |
		| Calculation | f7c14f5f-fa54-4699-ac16-dbfc76bd0189 | 91         | Primary IDACI Band F funding                                                                        |
		| Calculation | bd2a3406-38f7-4f55-b951-a6a736728880 | 346        | Primary IDACI Band F SEN %                                                                          |
		| Calculation | 250b33cd-4f77-499b-99a8-1bdb1a39d7f8 | 344        | Primary IDACI Band F SEN Total                                                                      |
		| Calculation | dbffcced-ad33-4ad8-a366-b654261805c2 | 96         | Secondary IDACI Band A Rate                                                                         |
		| Calculation | 2ebf2483-71bd-482e-a797-84498d6a7545 | 97         | Secondary IDACI Band A factor                                                                       |
		| Calculation | 44e8b811-ab6a-4efd-9bd7-12c50974320f | 98         | Secondary Pupil Count SBS                                                                           |
		| Calculation | 37a799bc-c8b0-47be-9ef4-7ef33f2adb37 | 95         | Secondary IDACI Band A funding                                                                      |
		| Calculation | a8b1680d-111b-4978-ae66-79a2ff342e18 | 349        | Secondary IDACI Band A SEN %                                                                        |
		| Calculation | 376decb8-a3d2-4afd-913d-b68a40bc4ce5 | 347        | Secondary IDACI Band A SEN Total                                                                    |
		| Calculation | 3993030d-63fa-48fa-a977-a05a9699321b | 100        | Secondary IDACI Band B Rate                                                                         |
		| Calculation | 8b82c0f6-590b-4e4a-a218-74fec8c324a0 | 101        | Secondary IDACI Band B Factor                                                                       |
		| Calculation | 17773fc6-0500-42b5-9d99-7b6ea66731cb | 99         | Secondary IDACI Band B funding                                                                      |
		| Calculation | c384271b-66a8-4a7a-8b99-70fb862603ce | 352        | Secondary IDACI Band B SEN %                                                                        |
		| Calculation | 9c761e8d-724b-4320-b437-99ffc2de1e4f | 350        | Secondary IDACI Band B SEN Total                                                                    |
		| Calculation | 50c7a295-a5f3-4076-b484-0498f3a597d6 | 104        | Secondary IDACI Band C Rate                                                                         |
		| Calculation | 24c394d9-172a-43e1-b6b8-ffc8f9eba592 | 105        | Secondary IDACI Band C Factor                                                                       |
		| Calculation | fb808f92-8370-4b09-972c-0674472f810a | 103        | Secondary IDACI Band C funding                                                                      |
		| Calculation | f175ab50-f70d-4ef5-a469-d5847e0c5770 | 356        | Secondary IDACI Band C SEN %                                                                        |
		| Calculation | 212326eb-71ba-474a-93d0-b3f0ff843a74 | 354        | Secondary IDACI Band C SEN Total                                                                    |
		| Calculation | 7bc842e8-3525-4db9-8e3c-788d2a99d5db | 108        | Secondary IDACI Band D Rate                                                                         |
		| Calculation | cfe5a04e-fa4a-4124-8df2-a7465b5acbe7 | 109        | Secondary IDACI Band D Factor                                                                       |
		| Calculation | 57d3a9d0-318d-4500-b3be-a9eeaf4973e0 | 107        | Secondary IDACI Band D funding                                                                      |
		| Calculation | c064520f-f84f-40e2-8db0-09573581ca40 | 359        | Secondary IDACI Band D SEN %                                                                        |
		| Calculation | e4e582e9-fc5f-4511-ab29-4d423b30ab2d | 357        | Secondary IDACI Band D SEN Total                                                                    |
		| Calculation | 418d7e1f-f8fe-49f5-b578-03015a5ecb33 | 112        | Secondary IDACI Band E Rate                                                                         |
		| Calculation | 3a3c4c11-63c7-4ea2-8731-9aaf324604e9 | 113        | Secondary IDACI Band E Factor                                                                       |
		| Calculation | f506ecc9-edce-40f3-8a1b-42f03f36e2e7 | 111        | Secondary IDACI Band E funding                                                                      |
		| Calculation | e3aa6c5f-57d6-4901-8132-5157aad9fb1c | 362        | Secondary IDACI Band E SEN %                                                                        |
		| Calculation | e9302611-d9c2-429e-b383-a9300004d64a | 360        | Secondary IDACI Band E SEN Total                                                                    |
		| Calculation | 21dcf091-b09b-46c0-b9f5-1f421992f741 | 116        | Secondary IDACI Band F Rate                                                                         |
		| Calculation | 31264a49-e83b-4908-8114-11628128194b | 117        | Secondary IDACI Band F Factor                                                                       |
		| Calculation | 9f176522-f981-4021-937c-8d6b3dca5f27 | 115        | Secondary IDACI Band F funding`                                                                     |
		| Calculation | f82e601f-128d-4a37-bcdb-d1677853e08c | 365        | Secondary IDACI Band F SEN %                                                                        |
		| Calculation | cb58d29c-f81a-4d89-adba-ff29076bfccb | 363        | Secondary IDACI Band F SEN Total                                                                    |
		| Calculation | 377038dd-e124-4afd-8bdc-e45f39150ebd | 120        | Primary FSM Rate                                                                                    |
		| Calculation | 204e9392-eff0-4433-b81a-31968e3b0a25 | 121        | Primary FSM Factor                                                                                  |
		| Calculation | 3d6c4eb8-2a8a-4db8-86a2-182194f4a419 | 119        | Primary free school meals (FSM) Funding                                                             |
		| Calculation | e8d20a05-4a2e-4543-b44a-daef1899e6ea | 368        | Primary free school meals (FSM) SEN %                                                               |
		| Calculation | cd2c6719-b190-4903-b29d-a7c8ebe9390a | 366        | Primary free school meals (FSM) SEN Total                                                           |
		| Calculation | 35a97833-624b-4589-9327-38a42803996e | 124        | Primary FSM6 Rate                                                                                   |
		| Calculation | dc370e8b-fc76-44cb-9e08-c4f57b631dcb | 125        | Primary FSM6 Factor                                                                                 |
		| Calculation | bd794e50-e32f-4075-92cf-53fec4c3a044 | 123        | Primary FSM6 Funding                                                                                |
		| Calculation | 50ce2c4c-86b8-4fd5-8fc9-c68aad616f14 | 371        | Primary FSM6 SEN %                                                                                  |
		| Calculation | 358e784b-179b-4eb9-bd72-8d0218466bec | 369        | Primary FSM6 SEN Total                                                                              |
		| Calculation | 54081115-96df-4f67-90aa-da23e1820933 | 130        | Secondary FSM Rate                                                                                  |
		| Calculation | b3172c58-c2e3-41e1-ac2a-fd9ca2dde72a | 131        | Secondary FSM Factor                                                                                |
		| Calculation | 2d75f7ca-c68c-4eaa-9c06-bca4802f39fd | 129        | Secondary free school meals (FSM) Funding                                                           |
		| Calculation | 6ec89196-f0b8-4fa1-8453-3555aeb3ac5d | 374        | Secondary free school meals (FSM) SEN %                                                             |
		| Calculation | fd2cf131-5488-4503-97f5-7697e5078381 | 372        | Secondary free school meals (FSM) SEN Total                                                         |
		| Calculation | b8baa2c5-24ab-4499-8a3e-1590bcaa5234 | 135        | Secondary FSM6 Rate                                                                                 |
		| Calculation | 7598ef03-1d86-4981-8a89-f44820fbfdd5 | 136        | Secondary FSM6 Factor                                                                               |
		| Calculation | 05261e95-bb9c-4f7b-88a1-f313c92d5e50 | 134        | Secondary FSM6 Funding                                                                              |
		| Calculation | a47c4e52-90f3-4b8d-8635-3a997fdd9660 | 377        | Secondary FSM6 SEN %                                                                                |
		| Calculation | dd6781ef-dc12-483f-910f-7ff0b010e995 | 375        | Secondary FSM6 SEN Total                                                                            |
		| Calculation | 1e74f84b-333d-4d0d-be49-9388ca6aa4aa | 139        | Looked After Children Rate                                                                          |
		| Calculation | 7bd5ea1b-55c2-45fb-9748-2c1dbfa010b1 | 140        | Looked After Children Factor                                                                        |
		| Calculation | 636c17a8-6b21-4939-8098-e804f2988e75 | 141        | Total Pupil Count SBS                                                                               |
		| Calculation | c4c2c50e-f669-46e1-8b55-f6075712c90f | 138        | Looked After Children Funding                                                                       |
		| Calculation | 41eca670-8db0-435d-8b9a-91567571eb3d | 380        | Looked After Children SEN %                                                                         |
		| Calculation | 2daed5a4-1720-4214-973a-3ab961ca9abc | 378        | Looked After Children SEN Total                                                                     |
		| Calculation | 5dbcd2e2-cb73-4941-9e5f-078aeec3feea | 145        | Primary Prior attainment rate                                                                       |
		| Calculation | 680881f6-b1c1-492b-8b70-561d28097267 | 146        | Primary Prior attainment factor                                                                     |
		| Calculation | a76f7851-062b-40d0-8db8-ce9e64b79538 | 144        | Primary Attainment - Low Primary Prior Attainment Funding                                           |
		| Calculation | 1e201223-3cb2-4351-8ec3-6ba9dd4850a8 | 383        | Primary Attainment - Low Primary Prior Attainment SEN %                                             |
		| Calculation | f2054f45-cf93-4175-9dd1-c7f15dda5407 | 381        | Primary Attainment - Low Primary Prior Attainment SEN Total                                         |
		| Calculation | b6215036-7af8-427d-8e47-231b9f5bc9e0 | 150        | Secondary Prior Attainment Rate                                                                     |
		| Calculation | 58183918-c2c4-4af8-9dd7-4afc66f589af | 151        | Secondary Prior Attainment Factor                                                                   |
		| Calculation | 477c37d4-fe93-4fb2-9244-613a0c3aebec | 149        | Secondary Attainment - Secondary Pupils not achieving the expected standards in KS2 Tests Funding   |
		| Calculation | 6d4e439b-fd0d-4968-a77e-b76db92584ae | 386        | Secondary Attainment - Secondary Pupils Not Achieving the expected standards in KS2 Tests SEN %     |
		| Calculation | a7838e21-fae1-475b-850b-83bfe37e0fb5 | 384        | Secondary Attainment - Secondary Pupils Not Achieving the expected standards in KS2 Tests SEN Total |
		| Calculation | fcdb6c77-434c-4b49-833b-a2629208adb7 | 154        | Primary EAL1 Rate                                                                                   |
		| Calculation | 2166eacc-edc9-4bed-a326-c0f4d32117e3 | 155        | Primary EAL1 Factor                                                                                 |
		| Calculation | fa51564f-914d-4d5a-9024-bf053a9c17c8 | 153        | Primary EAL Band 1 Funding                                                                          |
		| Calculation | f13433a0-eea7-41e6-9986-2dcb2bd328a3 | 389        | Primary EAL Band 1 SEN %                                                                            |
		| Calculation | 9fabe999-c4cc-45d2-b8d7-d2a5cf9f2c09 | 387        | Primary EAL Band 1 SEN Total                                                                        |
		| Calculation | 76fe4544-c380-4e7b-8544-6684f8f133b3 | 159        | Primary EAL2 Rate                                                                                   |
		| Calculation | b903444e-c0bf-4006-bd00-6d7865b5c1dd | 160        | Primary EAL2 Factor                                                                                 |
		| Calculation | e3fc8899-a630-431f-a36c-787fd9384826 | 158        | Primary EAL Band 2 Funding                                                                          |
		| Calculation | 4094630d-319e-4695-8d2c-d73fa4e696b4 | 392        | Primary EAL Band 2 SEN %                                                                            |
		| Calculation | 2c0b7bc0-b2b7-4d3e-9685-6deaae56d9f2 | 390        | Primary EAL Band 2 SEN Total                                                                        |
		| Calculation | e420c1d4-ff26-424f-8917-30ea72fdcd73 | 163        | Primary EAL3 Rate                                                                                   |
		| Calculation | e1178be4-5924-44f7-a8a8-8bb44819c570 | 164        | Primary EAL3 Factor                                                                                 |
		| Calculation | 058f22ea-ccc7-4f2d-9e2b-fffc6d7fba4b | 162        | Primary EAL Band 3 Funding                                                                          |
		| Calculation | a19df2b3-63d8-47d2-9405-c8863c7a34f7 | 396        | Primary EAL Band 3 SEN %                                                                            |
		| Calculation | 982693f4-c015-463f-ad0c-8c5e9c6ffab8 | 394        | Primary EAL Band 3 SEN Total                                                                        |
		| Calculation | f0ea2e21-1ade-4cb7-84ac-539952c6199c | 169        | Secondary EAL1 Factor                                                                               |
		| Calculation | c8898af8-188a-4154-809c-e50f6e230025 | 168        | Secondary EAL1 Rate                                                                                 |
		| Calculation | 78183215-4c23-4b4f-a9bd-9e5f8caf61ef | 167        | Secondary EAL Band 1 Funding                                                                        |
		| Calculation | 77e37a97-32c6-4326-876e-872143251629 | 401        | Secondary EAL Band 1 SEN %                                                                          |
		| Calculation | 1c3df7fb-a773-4f8d-83bb-2e4ab2ae50c4 | 398        | Secondary EAL Band 1 SEN Total                                                                      |
		| Calculation | cd841069-71f5-485b-a9fe-0372d3883306 | 172        | Secondary EAL2 Rate                                                                                 |
		| Calculation | 00e22126-05c9-4c3e-a6ff-1187e67f5090 | 173        | Secondary EAL2 Factor                                                                               |
		| Calculation | e6181805-68ac-480d-89ab-1b9642f6a9be | 171        | Secondary EAL Band 2 Funding                                                                        |
		| Calculation | 52000633-dc4f-47b1-8fed-af823e2c0ea3 | 404        | Secondary EAL Band 2 SEN %                                                                          |
		| Calculation | d01a0579-a898-4c11-9822-34c21aff3c42 | 402        | Secondary EAL Band 2 SEN Total                                                                      |
		| Calculation | fb5770ce-4079-4416-bb08-b9f5ff4ed39f | 176        | Secondary EAL3 Rate                                                                                 |
		| Calculation | a90f3e18-5674-4e11-8301-200a437d2de1 | 177        | Secondary EAL3 Factor                                                                               |
		| Calculation | af7b5468-275f-472b-a3d8-57bc9deea236 | 175        | Secondary EAL Band 3 Funding                                                                        |
		| Calculation | 40b62036-9bef-4c12-a5e9-ec0606dc1e23 | 408        | Secondary EAL Band 3 SEN %                                                                          |
		| Calculation | bd86816e-63fd-447c-9975-12b2e1013921 | 406        | Secondary EAL Band 3 SEN Total                                                                      |
		| Calculation | 89416254-2575-434b-bd58-ef9296ddff66 | 180        | Primary Mobility Rate                                                                               |
		| Calculation | 33c00845-bbe1-4d16-b191-d9cf7814622b | 181        | Primary Mobility Factor                                                                             |
		| Calculation | c75abe6b-3b8d-4872-ba87-48468bcba427 | 179        | Primary Mobility Funding                                                                            |
		| Calculation | 3417c52b-5fff-4e15-8fce-ba62684c787a | 412        | Primary Mobility SEN %                                                                              |
		| Calculation | 0aebb694-f29e-47f7-88d9-5c794a3695dc | 410        | Primary Mobility SEN Total                                                                          |
		| Calculation | 1f76dae7-d72a-42a0-9ec4-9bb106ae0156 | 184        | Secondary Mobility Rate                                                                             |
		| Calculation | 8138e75b-777e-44e8-9609-d3fde44f85b3 | 185        | Secondary Mobility Factor                                                                           |
		| Calculation | 67b09d13-ef01-4f8b-82bf-3c83bf0367b9 | 183        | Secondary Mobility Funding                                                                          |
		| Calculation | 6454f2d7-49a6-48f6-b4e6-745240bf2ea7 | 416        | Secondary Mobility SEN %                                                                            |
		| Calculation | 6ca9923c-86ef-4165-8c77-a0998f0a8b48 | 414        | Secondary Mobility SEN Total                                                                        |
		| Calculation | f2a302e2-5cf5-45f6-9944-0809da9e4f78 | 212        | Primary Lump Sum Value                                                                              |
		| Calculation | 111200c6-07c8-4424-967b-3b2c3d1d1b0f | 213        | Primary Lump Sum Factor                                                                             |
		| Calculation | 0eeeab9e-8a5e-40bd-9439-1e22ed6fa211 | 211        | Primary Lump Sum Funding                                                                            |
		| Calculation | e61cf665-47c0-4149-95fb-0f09de81f97d | 422        | Primary Lump Sum SEN %                                                                              |
		| Calculation | 7e238559-0d24-434c-8edb-c964e5e5ef3b | 420        | Primary Lump Sum SEN Total                                                                          |
		| Calculation | 87067f53-7eac-4e86-883f-3db34e5276cf | 215        | Secondary Lump Sum Factor                                                                           |
		| Calculation | 3ba29f60-eb82-433b-bb97-8f10d47e5cc9 | 216        | Secondary Lump Sum Value                                                                            |
		| Calculation | ac63be86-3b6f-4c77-9b15-e45fe925c9bf | 214        | Secondary Lump Sum Funding                                                                          |
		| Calculation | eb9d407e-24b6-4c4c-9b3a-1e0cf05c6e0c | 425        | Secondary Lump Sum SEN %                                                                            |
		| Calculation | 4c9cf10d-6d7c-4e3b-886d-d772b89e2814 | 423        | Secondary Lump Sum SEN Total                                                                        |
		| Calculation | 62eb6b69-1521-44d5-b58c-c2b3dcfc1e02 | 191        | Sparsity Distance                                                                                   |
		| Calculation | b5bfef59-1fd8-4475-b815-a7fae50a882c | 192        | Sparsity Distance Threshold                                                                         |
		| Calculation | 5c88baa2-f663-4982-bd11-488d276b54a9 | 196        | No of Primary Year Groups                                                                           |
		| Calculation | 162d56e6-8b92-46ac-a638-b387d4d79ce1 | 197        | No of Secondary Year Groups                                                                         |
		| Calculation | 9a6b6afb-751e-4291-a29f-0081122c647b | 194        | Average year group size                                                                             |
		| Calculation | fdfacd09-bebe-4fde-a2d0-a8685fc0ac37 | 190        | Sparsity Distance Evaluation                                                                        |
		| Calculation | a8973266-8d72-45cd-9cb8-e14f0870b57c | 193        | Average year group size evaluation                                                                  |
		| Calculation | cb8bb595-7cd1-4135-ba02-ed3d1395df27 | 189        | Sparsity Criteria Evaluation                                                                        |
		| Calculation | 2afe77ee-8168-4a07-95f1-f7b557206b63 | 207        | Sparsity Average Year Group Size Threshold                                                          |
		| Calculation | 40fbc5d3-6eb9-47f7-b834-91ba43169d18 | 209        | NFF Weighting evaluation                                                                            |
		| Calculation | d0d677a4-5f82-4714-81a6-d5f63aa8aa8e | 210        | Sparsity NFF Taper                                                                                  |
		| Calculation | 3c000f9b-4995-4903-81b0-e4a587f54dd8 | 202        | Sparsity Methodology                                                                                |
		| Calculation | 3e9d6f38-5098-4793-8268-bcd2c6753830 | 203        | Lump Sum Weighting                                                                                  |
		| Calculation | feebb937-77b4-4392-b1c6-f4bef176000c | 204        | Sparsity Taper Weighting                                                                            |
		| Calculation | 8a73996f-6983-4785-9dd2-3c3706c928b9 | 208        | Sparsity NFF Weighting                                                                              |
		| Calculation | 2f6a0828-2d2c-4bbf-8bc9-150256f594f1 | 201        | Sparsity Methodology Weighting Selection                                                            |
		| Calculation | 6bc558ab-b158-44b8-b7f8-7a6befbf89bb | 188        | Sparsity Evaluation                                                                                 |
		| Calculation | 5317c77e-07a1-4a62-9109-12ab581101a5 | 199        | Sparsity Lump Sum                                                                                   |
		| Calculation | ff122d10-4963-4b27-b7b6-99c046d898b4 | 200        | Sparsity Weighting                                                                                  |
		| Calculation | a26fe811-91e0-4b9d-9fe9-613d445ceb77 | 187        | Sparsity Funding                                                                                    |
		| Calculation | 83945ff2-76f7-4b4a-addc-cd2a13069063 | 419        | Sparsity SEN %                                                                                      |
		| Calculation | 09408dd6-45a1-4ce0-bbb6-119a85e3aff3 | 417        | Sparsity SEN Total                                                                                  |
		| Calculation | 186490d0-8f6c-4c0b-ad41-8b06ca2adc74 | 564        | Split site weighting                                                                                |
		| Calculation | 2a0f0f2d-b945-4e80-97cf-37cba8fa32a5 | 217        | Split Site total                                                                                    |
		| Calculation | 5f94bc40-debf-49ce-8cb8-f399de31a9fb | 428        | Split Site Full year SEN %                                                                          |
		| Calculation | 0f4f02c5-9d56-48f0-9c26-0d89418e5641 | 426        | Split Site Full year SEN Total                                                                      |
		| Calculation | 433f02eb-4d12-497b-9fb6-67f23daab6b4 | 220        | Front Loaded PFI Indicator                                                                          |
		| Calculation | c496d274-15cb-450e-93aa-ccc1c1d210b1 | 221        | PFI APT Data                                                                                        |
		| Calculation | 41fd7633-babd-437d-89a8-7a6756a77463 | 219        | PFI Type value selection                                                                            |
		| Calculation | d4b7bedc-98bd-4b9a-9dbc-b6b61ee00625 | 565        | PFI weighting                                                                                       |
		| Calculation | 56ed9478-ad68-4d4d-afcd-ce96f633d963 | 218        | Standard PFI                                                                                        |
		| Calculation | 9016b5de-cd6e-4025-9bae-92cdd16560fd | 431        | Standard PFI SEN %                                                                                  |
		| Calculation | d5d1d35c-459a-4036-9589-ad6a4bc96348 | 429        | Standard PFI SEN Total                                                                              |
		| Calculation | 3f8a09a0-4fc2-4346-9f34-b26b0d9b8bff | 84         | Primary IDACI Band D Rate                                                                           |
		| Calculation | d4a02087-e3a9-4498-8dc3-b1662a587b60 | 85         | Primary IDACI Band D Factor                                                                         |
		| Calculation | 11fbe321-1b2b-4221-bc18-67ebc07225d3 | 83         | Primary IDACI Band D funding                                                                        |
		| Calculation | d6be331a-253f-4363-a375-bafca45f4879 | 228        | Exceptional Circumstance 1 Funding                                                                  |
		| Calculation | 58d9d140-5a7b-4039-8877-f0d2aa71159a | 236        | London Fringe Eligible Funding                                                                      |
		| Calculation | f41ab80c-e39b-43bc-aabd-6aeb70fc38fa | 223        | London Fringe Funding                                                                               |
		| Calculation | 0e5c8575-434c-44b3-b62a-52b3cf90cde2 | 235        | Prior Year Adjustment to SBS                                                                        |
		| Calculation | 3378a1bb-8a56-4ab1-a608-f118dd32c036 | 511        | APT NEWISB Rates                                                                                    |
		| Calculation | 63e772cc-01c4-404e-88c1-e0b0e676ab28 | 512        | APT Approved Additional Premises costs to exclude                                                   |
		| Calculation | 3a2af9de-e573-4e24-b0cd-577ddc231c22 | 241        | SBS Base Allocation                                                                                 |
		| Calculation | cf92aa4f-03f3-4cd3-b086-a12b43039226 | 239        | MFL Rate                                                                                            |
		| Calculation | 38625ca7-076e-4da0-bee9-745306951fd8 | 240        | SBS Per Pupil Allocation                                                                            |
		| Calculation | bb412013-aa9b-4568-8309-d37420932eab | 226        | MFL Evaluation                                                                                      |
		| Calculation | 1e51c13a-b2a7-462a-b31c-649034f7819e | 225        | MFL Uplift per pupil                                                                                |
		| Calculation | 141bfd4a-31a9-41b0-a2d2-bc6237abd590 | 560        | MFL Adjustment SEN %                                                                                |
		| Calculation | 52262e18-d970-4a63-a022-d6a2cd5b99d0 | 227        | MFL Adjustment                                                                                      |
		| Calculation | 9a06c82b-80ae-4620-b8bf-1d976b98cfc4 | 434        | Exceptional Circumstances 1 SEN %                                                                   |
		| Calculation | 08fa534f-d2bb-409d-9d0b-46ee9891500d | 432        | Exceptional Circumstances 1 SEN Total                                                               |
		| Calculation | d3acda35-dde9-4fbc-807e-e24ed3bdb28b | 229        | Exceptional Circumstance 2 Funding                                                                  |
		| Calculation | 2826e609-f187-4912-9a89-23c8c93f98e8 | 438        | Exceptional Circumstances 2 SEN %                                                                   |
		| Calculation | 4ce5468d-bda6-4d5a-bae5-d958d4c99508 | 436        | Exceptional Circumstances 2 SEN Total                                                               |
		| Calculation | f0951107-9c48-4f37-897c-981dada3efc9 | 230        | Exceptional Circumstances 3 Funding                                                                 |
		| Calculation | 7a4dc0d7-5aa5-4ec4-8e9a-72f5209c5ac7 | 442        | Exceptional Circumstances 3 SEN %                                                                   |
		| Calculation | 670cd089-f292-4539-b900-64534f5eccf3 | 440        | Exceptional Circumstances 3 SEN Total                                                               |
		| Calculation | ef7b1a48-815d-49b6-9b36-ec604fbfb72b | 231        | Exceptional Circumstance 4 Funding                                                                  |
		| Calculation | c2e0d2f8-2277-4445-a1f9-eca963b78a4c | 446        | Exceptional Circumstances 4 SEN %                                                                   |
		| Calculation | 5ad44b8c-97d9-4f8f-927a-d242240b9df7 | 444        | Exceptional Circumstances 4 SEN Total                                                               |
		| Calculation | c6d9191a-9268-4246-86d8-596ca99124e8 | 232        | Exceptional Circumstance 5 Funding                                                                  |
		| Calculation | 3e4ff4f7-b78c-4821-b559-40b3ee581e37 | 451        | Exceptional Circumstances 5 SEN %                                                                   |
		| Calculation | 919af526-2140-4ce8-a03e-9e478bd7a52e | 448        | Exceptional Circumstances 5 SEN Total                                                               |
		| Calculation | ad74e7af-16a3-47f9-9bbc-657f3062293f | 233        | Exceptional Circumstances 6 Funding                                                                 |
		| Calculation | 596f79fa-f67e-4b47-a945-7bd65b223630 | 454        | Exceptional Circumstances 6 SEN %                                                                   |
		| Calculation | 2fad3459-ba4a-4eaa-a6e7-d8ba9d38261b | 452        | Exceptional Circumstances 6 SEN Total                                                               |
		| Calculation | 3eb7be55-f247-4803-962f-b2c0676581b6 | 234        | Exceptional Circumstance 7 Funding                                                                  |
		| Calculation | 88cbb66c-a32c-47bc-bc4b-9894b72e6691 | 458        | Exceptional Circumstances 7 SEN %                                                                   |
		| Calculation | 1a1ba3be-f479-454c-a8d2-a0fe9c99e192 | 456        | Exceptional Circumstances 7 SEN Total                                                               |
		| Calculation | 31c653cf-af45-4990-9c91-ace572bbba12 | 520        | Previous Full Year SBS Total                                                                        |
		| Calculation | 14718456-a287-4722-a56f-60be7916e415 | 521        | Current Year Lump Sum Including Fringe                                                              |
		| Calculation | 683b3d1e-c656-444f-ae45-837c9756b19e | 522        | Current Year Sparsity Total                                                                         |
		| Calculation | 2cfdb755-89c4-49d1-8854-a1aa439e4b44 | 523        | Previous Year Approved MFG Exclusions Total                                                         |
		| Calculation | c855e356-54ee-4f2d-a866-95a99c3db76a | 524        | Total Technical Adjustment to Baseline Year Funding                                                 |
		| Calculation | 0f3836da-1b14-49e9-aed9-9a058c04f9c6 | 712        | Baseline Current Year Lump Sum Including Fringe                                                     |
		| Calculation | 12ae9f9a-d9fd-4852-8ed9-114503ba9e6b | 519        | Adjusted Previous Year School Budget Share                                                          |
		| Calculation | f6281310-00aa-42f0-a4a4-39541c3c4caa | 525        | Previous Year Total Pupil Count MFG                                                                 |
		| Calculation | 55c78b2d-1340-45a5-846f-8d614dc55c92 | 518        | Previous Year MFG Unit Value                                                                        |
		| Calculation | e1a30933-0fad-49ec-beb3-34038d2fb5b2 | 526        | MFG Floor                                                                                           |
		| Calculation | 9a3f57ef-d715-4ce2-bd94-45e03d11cbfa | 527        | MFG Floor Constant                                                                                  |
		| Calculation | a6c8f2f9-1d10-487c-a6bc-d6fda82e3f5c | 693        | Current year MFG Primary pupil count                                                                |
		| Calculation | 9fcda229-77a0-4578-ae98-af5608d9c654 | 694        | Current year MFG secondary pupil count                                                              |
		| Calculation | a90800cd-bdcf-425b-8916-1b8649abc8ac | 517        | Minimum Value Per pupil                                                                             |
		| Calculation | c9c068a7-142a-457c-8e92-bddd8cc6f8e4 | 528        | Current Year Total MFG Pupil Count                                                                  |
		| Calculation | 0dd75339-0803-46e5-af5d-2b012d19c4ce | 530        | Current Year SBS Total                                                                              |
		| Calculation | 8079034a-6c5d-44f0-bd3e-6ead8e94bb56 | 533        | Current Year Approved MFG Exclusions Total                                                          |
		| Calculation | e864becd-892d-4656-912a-48f4802bce76 | 534        | Total Current Year Technical Adjustments                                                            |
		| Calculation | a7fe509d-b9e6-4dc8-a481-7291fb7eab95 | 516        | Current year Guaranteed level of funding                                                            |
		| Calculation | 9c09a321-eca4-497b-bb41-9667a8d6bcfa | 529        | Total Current Year MFG Budget                                                                       |
		| Calculation | 72e24f7e-5dcf-4c2e-b88c-4277d8efbad4 | 515        | MFG Adjustment evaluation                                                                           |
		| Calculation | fe3bcb3b-3998-40bc-91fe-802110ab59d3 | 692        | Extent to which percentage change falls below MFG floor                                             |
		| Calculation | 38fedaa4-4f2b-435e-a457-0044513ae4d2 | 541        | Current Year MFG unit Value                                                                         |
		| Calculation | 0b4a43d4-88f7-47fa-89d5-7e17113739e0 | 540        | Change in MFG Unit Value                                                                            |
		| Calculation | cfbb4880-21ee-4265-8918-b61ca48b8af2 | 544        | Growing School Check                                                                                |
		| Calculation | d7b2b451-4e05-4bd6-9fe2-2fe9d082f1d5 | 539        | Percentage change in MFG unit value                                                                 |
		| Calculation | 698822c6-ba43-424f-ad6d-726ff81aa95b | 542        | LA Capping Factor                                                                                   |
		| Calculation | bff2812a-a128-405c-ad87-5a917b986853 | 543        | Growing School Evaluation                                                                           |
		| Calculation | 174e95d0-11cd-4d56-8d5e-8edcf0faa9f8 | 713        | Does the LA apply capping and scaling                                                               |
		| Calculation | 0e137c2c-d55e-4f76-ab21-eab66d7b0ba6 | 538        | Cap percentage change evaluation                                                                    |
		| Calculation | 31e97413-c3a3-4f91-846d-1481e2ea71f9 | 537        | Extent to which percentage change exceed the cap                                                    |
		| Calculation | 1df9c246-57ad-4987-a598-a7c16e528b14 | 545        | LA Scaling Factor                                                                                   |
		| Calculation | 5e4d20e9-ff72-4796-86a2-22f17444dce0 | 536        | Scaled Factor Applied to Excess Above Cap                                                           |
		| Calculation | b1c698f3-dd53-478b-a488-92fba43236e6 | 535        | Affordability Adjustment Value                                                                      |
		| Calculation | 59d5f9df-8082-42ca-8385-0d602322c926 | 554        | MFL Funding                                                                                         |
		| Calculation | 7765d8a9-5c06-42d7-86ae-d43002a73d4c | 555        | APT Premises Exceptions                                                                             |
		| Calculation | 2057be6e-ea03-4262-8c93-076725659d86 | 556        | PFI                                                                                                 |
		| Calculation | 7df29588-9a41-4375-b205-530604ada14b | 552        | MFL Check Value Constant                                                                            |
		| Calculation | 4fba8423-b5fe-41e7-96d5-125b23709286 | 553        | Minimum Funding                                                                                     |
		| Calculation | 7ea4ea56-7df3-42bd-9561-0f1ca2240328 | 558        | MFL To check if I can clone                                                                         |
		| Calculation | 274a745b-cbf6-4852-a09b-c7c606d92298 | 550        | MFL Check Value Evaluation                                                                          |
		| Calculation | ca588a55-e282-4ebc-a23f-e4aaff6f17a5 | 557        | Actual MFL Eligible Funding                                                                         |
		| Calculation | 4aef4fa3-eff9-4216-9cb6-d553e163f8fb | 547        | Affordability Adjustment evaluation                                                                 |
		| Calculation | 3e636fc2-10c6-4996-a143-bda19fef4fa8 | 548        | MFL Check Value                                                                                     |
		| Calculation | 4ca8af79-2dc4-4a76-94c1-4dd05cde9c81 | 514        | MFG Adjustment Value                                                                                |
		| Calculation | d65aae08-2b13-45a0-985d-0de04b5b5386 | 546        | Minimum Funding Level Adjustment post cap and scaling                                               |
		| Calculation | 7af1f923-4336-466f-bf56-773403c82598 | 513        | MFG Overall Net Cash Adjustment                                                                     |
		| Calculation | 7e282e12-6f81-408e-be6d-ff693f498635 | 562        | MFG Overall Net Cash SEN %                                                                          |
		| Calculation | 679d4d47-1eaf-4dc0-be20-0370600fefec | 466        | Rate Unoccupied Place Rate                                                                          |
		| Calculation | abd5d8cd-2382-4347-9348-062dd27d8906 | 467        | Special Unoccupied Pre 16 Places                                                                    |
		| Calculation | 7832f65c-5be3-4905-9592-029df8f258b4 | 732        | Proportion of year open                                                                             |
		| Calculation | d07eede9-9a1d-46ac-83d2-91671737d41a | 567        | Days open in year                                                                                   |
		| Calculation | 1110f308-d7f1-4364-b4bd-d0ea050c9864 | 465        | Special Unoccupied Total                                                                            |
		| Calculation | eefd8b04-14a6-49b5-9943-5a10864a7929 | 654        | Special unoccupied - Full year subtotal                                                             |
		| Calculation | bd40f3cb-7b04-4aa7-a146-40f40f3a2f64 | 469        | Rate Occupied Place Rate                                                                            |
		| Calculation | 5151e6c2-6274-4283-ba7d-44c9357612a3 | 470        | Special Occupied Pre 16 Places                                                                      |
		| Calculation | 91be94f0-a918-4ac5-9a7e-3426045202b3 | 468        | Special Occupied Total                                                                              |
		| Calculation | 287d66e5-350c-445f-bd3b-81153de5501d | 656        | Special Occupied - Full year subtotal                                                               |
		| Calculation | be6a08ac-2103-4fcd-b926-385873fdabe1 | 477        | Alternative Provision Rate                                                                          |
		| Calculation | e9d7c408-31a0-4bd4-a33a-9e35b172bf3a | 478        | Alternative Provision Pre 16 Places                                                                 |
		| Calculation | 42020112-29ac-4f09-a93f-897c5da99b2d | 476        | Alternative Provision Total                                                                         |
		| Calculation | 7cf0d5c7-f00b-4943-81d4-3181a449c7fc | 658        | Alternative Provision - Full year subtotal                                                          |
		| Calculation | afe4b7fd-1e0c-4e53-95f5-a4389ddbac89 | 730        | Hospital Places                                                                                     |
		| Calculation | 1238c139-e22f-434b-975a-d34697557ab4 | 464        | Hospital Provision Funding                                                                          |
		| Calculation | c1e2f5b5-b7b2-4ef3-8388-0b37ac03eb88 | 662        | Hospital Provision - Full year subtotal                                                             |
		| Calculation | cbc46773-412d-4439-a590-56d0e99bcafa | 659        | Pre-16 High Needs - Full year subtotal                                                              |
		| Calculation | 67a08e21-95fb-4b94-b2f4-d523228e4849 | 459        | Post Opening Grant - Per Pupil Resources                                                            |
		| Calculation | 1423eae9-cec9-4749-a23b-c15ac7f82e8a | 460        | Post Opening Grant - Leadership Diseconomies                                                        |
		| Calculation | d00a00ce-0a84-431a-9652-965b7dc42a04 | 461        | Start up Grant Part A                                                                               |
		| Calculation | be54fbce-c20e-41aa-9d5a-f8781cb40718 | 462        | Start up Grant Part B                                                                               |
		| Calculation | d4a5fc65-ead4-4dc2-b04e-a958eed65d5d | 566        | Basic Entitlement - Primary Full Year subtotal                                                      |
		| Calculation | dd9751cc-af3f-40b6-83d5-c22e2c7f291a | 568        | Basic Entitlement KS3 full year subtotal                                                            |
		| Calculation | 535ce184-f3c7-403d-88f4-a59368e4e02c | 570        | Basic Entitlement - KS4 Full Year Subtotal                                                          |
		| Calculation | 2d67068b-0d4b-41c1-be26-2a8ebde210cb | 571        | Primary IDACI Band A Full Year Subtotal                                                             |
		| Calculation | e343b36a-4f08-484d-8167-ecc612bd2c56 | 572        | Primary IDACI Band B - Full Year Subtotal                                                           |
		| Calculation | abdf18bb-7302-4adb-ab01-1789435f1f63 | 573        | Primary IDACI Band C - Full Year Subtotal                                                           |
		| Calculation | 266cbd73-06e7-481c-80c1-1491418b5906 | 575        | Primary IDACI Band D - Full Year Subtotal                                                           |
		| Calculation | d14b5ea4-3d0f-4fe1-87d2-5d00954388e9 | 576        | Primary IDACI Band E - Full year subtotal                                                           |
		| Calculation | 39878aab-f03c-468d-973d-dcc2ae45a23b | 577        | Primary IDACI Band F - Full year subtotal                                                           |
		| Calculation | 51102ce4-dca9-43dc-8ee1-d5d467ea2752 | 583        | Secondary IDACI Band A - Full year subtotal                                                         |
		| Calculation | 2a9b0f24-2001-4074-b987-dd32fcf610f5 | 585        | Secondary IDACI Band B - Full year subtotal                                                         |
		| Calculation | 020bf6b3-b537-4a59-a432-9ed366cf43b5 | 587        | Secondary IDACI Band C - Full year subtotal                                                         |
		| Calculation | 4260a95a-7e89-4646-80ed-7611b9596bf3 | 588        | Secondary IDACI Band D - Full Year Subtotal                                                         |
		| Calculation | 77d7c952-5c8f-4951-a1ac-1018263b27bc | 590        | Secondary IDACI Band E - Full year subtotal                                                         |
		| Calculation | 9dc41696-4c4d-47bd-b8e4-11a012b311fc | 592        | Secondary IDACI Band F - Full year subtotal                                                         |
		| Calculation | b95f2a8c-2f19-4cfc-adfc-124f828dc4b2 | 593        | Primary free school meals - Full year subtotal                                                      |
		| Calculation | 24b29c3d-a165-4c39-a33b-9b5860003ec5 | 596        | Primary FSM6 - Full year subtotal                                                                   |
		| Calculation | f0afc9a0-f6bc-4a19-ae1b-6ad8e8cb0d1b | 598        | Secondary Free School meals - Full year subtotal                                                    |
		| Calculation | b901b173-1cea-471a-9e5d-feb8aed7bddb | 601        | Secondary FSM6 - Full year subtotal                                                                 |
		| Calculation | 9c096a86-f468-4787-bf6f-edcd85b78a8c | 616        | Primary prior attainment - Full year subtotal                                                       |
		| Calculation | 941f848d-0c98-4365-8676-4dbc376b02af | 620        | Secondary prior attainment - Full year subtotal                                                     |
		| Calculation | 4f833685-26ed-4c23-b025-95158146ed39 | 621        | Primary EAL Band 1 - Full Year Subtotal                                                             |
		| Calculation | b5889a12-97bd-4a74-aa49-9d2d4471b3bd | 623        | Primary EAL Band 2 - Full year subtotal                                                             |
		| Calculation | 4b2999f9-e16b-4150-88b0-29ea6ce08afc | 625        | Primary EAL Band 3 - Full year subtotal                                                             |
		| Calculation | ae768083-e202-48af-93ba-c2ae41f6064a | 628        | Secondary EAL Band 1 - Full year subtotal                                                           |
		| Calculation | 1882032c-da6c-4d3b-b0f2-7789ea527954 | 630        | Secondary EAL Band 2 - Full year subtotal                                                           |
		| Calculation | ffcc3dee-7255-463e-8e14-7bae34283d4f | 632        | Secondary EAL Band 3 - Full year subtotal                                                           |
		| Calculation | b04ae7a0-4783-499b-af58-a7dcf3926e0a | 634        | Primary Mobility - Full year subtotal                                                               |
		| Calculation | 12e0caf0-89e6-4e14-bf7e-ee5900233611 | 636        | Secondary Mobility - Full year subtotal                                                             |
		| Calculation | 8219ab37-2ea3-44bf-acf7-c2e3500ccb20 | 579        | Basic Entitlement - Full Year Subtotal                                                              |
		| Calculation | 37df406f-af2d-4c14-b5e9-c737f752e8b0 | 581        | Deprivation - Full Year Subtotal                                                                    |
		| Calculation | 2174ef51-f916-4354-919a-2e05465fd66c | 603        | Looked After Children - Full year subtotal                                                          |
		| Calculation | 0706444a-261b-4c38-aa24-93c1257d5b01 | 614        | Prior attainment - Full year subbtotal                                                              |
		| Calculation | 8a8c6c57-96ff-47c4-b97f-219c76989251 | 633        | EAL Funding - Full year subtotal                                                                    |
		| Calculation | aff5b63f-53c0-45d2-a054-29275219adfe | 638        | Mobility Funding - Full year subtotal                                                               |
		| Calculation | 50ac16e0-2820-430d-817a-9ea39552d1b6 | 645        | Primary Lump Sum - Full year subtotal                                                               |
		| Calculation | fa52c84c-84b4-4027-a4ec-365810e9dc0f | 688        | Secondary Lump Sum - Full year subtotal                                                             |
		| Calculation | 41eddf91-3af1-4ccc-aac3-6e8810785ee4 | 643        | Sparsity Funding - Full year subtotal                                                               |
		| Calculation | 630f6425-56d3-45f6-8b74-3c51e154f9a6 | 686        | Lump sum - Full Year subtotal                                                                       |
		| Calculation | 21881ce6-13c0-4112-aa06-a65ed54288e6 | 723        | Split Site - Full Year subtotal                                                                     |
		| Calculation | 3ec8ff2e-fd60-40ca-84dc-9d45821bd845 | 725        | PFI - Full year subtotal                                                                            |
		| Calculation | ad49eea6-3370-448a-8730-1f8884338956 | 721        | London Fringe - Full year subtotal                                                                  |
		| Calculation | ed5cff68-0b99-49f1-8205-0826bede11f6 | 726        | MFL - Full year subtotal                                                                            |
		| Calculation | d15afb54-3321-4820-9410-c1adf37f6ca6 | 222        | Front Loaded PFI                                                                                    |
		| Calculation | 82588b02-5fa5-4b50-8455-e91bbdea72ae | 641        | PFI Funding - Full year subtotal                                                                    |
		| Calculation | 9c3d1b02-c585-4b43-a09c-90d6339b8f61 | 647        | Exceptional Circumstance 1 - Full year subtotal                                                     |
		| Calculation | 01dfefd2-5a09-4735-adcc-39a4a23cb482 | 649        | Exceptional Circumstances 2 - Full year subtotal                                                    |
		| Calculation | 597927ef-6020-441a-a8ac-d8517f36cb62 | 651        | Exceptional Circumstances 3 - Full year subtotal                                                    |
		| Calculation | 7a4f3a8a-6553-440a-8616-88d63dba1410 | 674        | Exceptional Circumstance 4 - Full year subtotal                                                     |
		| Calculation | 858ce70f-37a5-47c3-aac2-fd226a5cc1dc | 714        | Exceptional Circumstance 5  - Full year subtotal                                                    |
		| Calculation | a38af01d-625d-445b-8eea-f95b3cff1847 | 716        | Exceptional Circumstances 6 - Full year subtotal                                                    |
		| Calculation | 71de43e4-ec29-4646-ad54-301a6425e286 | 715        | Exceptional Circumstance 7 - Full year subtotal                                                     |
		| Calculation | 1ef435f2-ec31-4f30-9711-42ffe5e91d68 | 683        | Prior year adjustment to SBS - Full year subtotal                                                   |
		| Calculation | 1675164c-0f2c-442e-81c4-a1bd65902f80 | 640        | Pupil Led Factors - Full year subtotal                                                              |
		| Calculation | 50ee9a48-fb65-4d23-a750-94bcfb4c9488 | 727        | Other Factors excluding MFL - Full year subtotal                                                    |
		| Calculation | b759aeda-ef33-4cae-926a-a7dc61e5d58a | 720        | Other Factors excluding MFL                                                                         |
		| Calculation | 2c75243f-0e91-4e0a-94f3-0c7cb0d0c031 | 696        | APT MFG Funded Pupil Numbers                                                                        |
		| Calculation | def15a33-3a0a-4802-949a-54413bf7441e | 698        | Original APT MFG adjustment value                                                                   |
		| Calculation | 3135e370-8684-42e0-aa38-e0667a0cf146 | 702        | Current Year MFG after MFG and affordability adjustment                                             |
		| Calculation | f9084246-144a-48e8-b5a7-747b8dbb0507 | 701        | Current Year MFG Unit Value after MFG and affordability adjustment                                  |
		| Calculation | 973e75bd-6d27-404e-baf0-3e30a568c20e | 695        | In year opener overall net adjustment                                                               |
		| Calculation | 46456238-2f0e-404f-9ae2-39110121e33d | 699        | School budget share including MFG                                                                   |
		| Calculation | cb6ffea4-c6d0-48f8-ac15-6afb8bc92345 | 700        | Percentage Change in MFG Unit Value after Adjustment                                                |
		| Calculation | aae368bc-dd3f-433b-9992-68f4f296538f | 731        | Previous Year SUG POG Funding                                                                       |
		| Calculation | 8372cc73-910c-4fce-8e5f-0033930e14db | 610        | LA overall per pupil rate Current year                                                              |
		| Calculation | 99530590-73a4-4fb4-b4ad-a2e6ba72a9c0 | 611        | LA overall per pupil rate Previous year                                                             |
		| Calculation | 2bb6982f-862d-4c81-b245-cb05777131fa | 607        | Funding Protection floor                                                                            |
		| Calculation | 892813a3-d28f-470e-9bac-71e352ebd0ef | 608        | Extent below funding protection floor                                                               |
		| Calculation | 5bd17eaf-37bf-458c-aa17-79b4eecbc3cf | 609        | Percentage change in LA per pupil rates                                                             |
		| Calculation | b7669ac2-1fde-4e32-8e51-4392f980eb4c | 606        | Funding protection Evaluation                                                                       |
		| Calculation | 7e099955-e274-4f4e-a046-0eb4fd17a2cf | 613        | Difference per pupil                                                                                |
		| Calculation | 35d7efc0-23c2-4afe-8f44-e2be41ae9030 | 463        | Free School Protection                                                                              |
		| Calculation | 49d87089-de9b-4dbe-9ed9-cf0685e5316a | 604        | Free school protection - Full year subtotal                                                         |
		| Calculation | 1c407603-5b6f-4399-bdd2-dcec417d574e | 728        | School Budget Share - Full year subtotal                                                            |
		| Calculation | 5cc20f7f-b6a3-450c-8cbd-67ee9caed7e8 | 706        | De-Delegated funding retained by the LA                                                             |
		| Calculation | 6924a9a7-9375-471a-bae3-8c4f22f658de | 707        | De-Delegated funding retained by the LA - Full year subtotal                                        |
		| Calculation | 854ed3e8-a3e1-4be8-97ed-2d2864a2178b | 661        | Total High Needs - Full Year subtotal                                                               |
		| Calculation | d63850aa-fb2c-4ea3-a1b5-c86e2ae473f8 | 729        | Notional SEN funding - Full year subtotal                                                           |
		| Calculation | 27419dfd-a00c-43c0-9ce3-334d99da0b81 | 709        | Funding previously De-Delegated - Full year subtotal                                                |
		| Calculation | c6f06134-08ef-426b-9a7c-9dd981e80c79 | 719        | Funding previously de-delegated                                                                     |
		| Calculation | 880e79c8-4a01-46d6-abe1-1068fffc68b7 | 710        | Funding Basis                                                                                       |
		| Calculation | 394980d6-c1bc-4885-880c-50a8529c3b2f | 711        | Phase of education                                                                                  |
		| Calculation | b62be92d-b2e2-487f-87cc-0d974F9dd6ee | 738        | MFL Adjustment SEN Total                                                                            |
		| Calculation | 109d4ba4-6cdb-4ea7-841d-78185452cac9 | 739        | MFG Overall Net Cash SEN Total                                                                      |
		| Calculation | 0c2e6487-6dca-4cb3-9C07-2106d240a661 | 735        | Total Exceptional Circumstances Full year subtotal                                                  |
		| Calculation | a955c2da-ad0e-42cd-abf6-0ccbe31c834e | 734        | Teachers' pay and pension grant                                                                     |
		| Calculation | 73bec0b8-7bb2-473e-90e2-3a0238babf04 | 733        | Days in the year                                                                                    |
		| Calculation | d4b63faf-d2ea-40dc-b94a-8b1cf45bd882 | 737        | MFG Pupil Adjustment multiplier                                                                     |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 703                   | 320   |
		| 59                    | 320   |
		| 60                    | 320   |
		| 61                    | 320   |
		| 238                   | 320   |
		| 58                    | 320   |
		| 314                   | 320   |
		| 237                   | 320   |
		| 312                   | 320   |
		| 63                    | 320   |
		| 64                    | 320   |
		| 66                    | 320   |
		| 62                    | 320   |
		| 317                   | 320   |
		| 315                   | 320   |
		| 68                    | 320   |
		| 69                    | 320   |
		| 70                    | 320   |
		| 67                    | 320   |
		| 321                   | 320   |
		| 319                   | 320   |
		| 72                    | 320   |
		| 73                    | 320   |
		| 71                    | 320   |
		| 324                   | 320   |
		| 322                   | 320   |
		| 76                    | 320   |
		| 77                    | 320   |
		| 75                    | 320   |
		| 327                   | 320   |
		| 325                   | 320   |
		| 80                    | 320   |
		| 81                    | 320   |
		| 79                    | 320   |
		| 330                   | 320   |
		| 328                   | 320   |
		| 340                   | 320   |
		| 338                   | 320   |
		| 88                    | 320   |
		| 89                    | 320   |
		| 87                    | 320   |
		| 343                   | 320   |
		| 341                   | 320   |
		| 92                    | 320   |
		| 93                    | 320   |
		| 91                    | 320   |
		| 346                   | 320   |
		| 344                   | 320   |
		| 96                    | 320   |
		| 97                    | 320   |
		| 98                    | 320   |
		| 95                    | 320   |
		| 349                   | 320   |
		| 347                   | 320   |
		| 100                   | 320   |
		| 101                   | 320   |
		| 99                    | 320   |
		| 352                   | 320   |
		| 350                   | 320   |
		| 104                   | 320   |
		| 105                   | 320   |
		| 103                   | 320   |
		| 356                   | 320   |
		| 354                   | 320   |
		| 108                   | 320   |
		| 109                   | 320   |
		| 107                   | 320   |
		| 359                   | 320   |
		| 357                   | 320   |
		| 112                   | 320   |
		| 113                   | 320   |
		| 111                   | 320   |
		| 362                   | 320   |
		| 360                   | 320   |
		| 116                   | 320   |
		| 117                   | 320   |
		| 115                   | 320   |
		| 365                   | 320   |
		| 363                   | 320   |
		| 120                   | 320   |
		| 121                   | 320   |
		| 119                   | 320   |
		| 368                   | 320   |
		| 366                   | 320   |
		| 124                   | 320   |
		| 125                   | 320   |
		| 123                   | 320   |
		| 371                   | 320   |
		| 369                   | 320   |
		| 130                   | 320   |
		| 131                   | 320   |
		| 129                   | 320   |
		| 374                   | 320   |
		| 372                   | 320   |
		| 135                   | 320   |
		| 136                   | 320   |
		| 134                   | 320   |
		| 377                   | 320   |
		| 375                   | 320   |
		| 139                   | 320   |
		| 140                   | 320   |
		| 141                   | 320   |
		| 138                   | 320   |
		| 380                   | 320   |
		| 378                   | 320   |
		| 145                   | 320   |
		| 146                   | 320   |
		| 144                   | 320   |
		| 383                   | 320   |
		| 381                   | 320   |
		| 150                   | 320   |
		| 151                   | 320   |
		| 149                   | 320   |
		| 386                   | 320   |
		| 384                   | 320   |
		| 154                   | 320   |
		| 155                   | 320   |
		| 153                   | 320   |
		| 389                   | 320   |
		| 387                   | 320   |
		| 159                   | 320   |
		| 160                   | 320   |
		| 158                   | 320   |
		| 392                   | 320   |
		| 390                   | 320   |
		| 163                   | 320   |
		| 164                   | 320   |
		| 162                   | 320   |
		| 396                   | 320   |
		| 394                   | 320   |
		| 169                   | 320   |
		| 168                   | 320   |
		| 167                   | 320   |
		| 401                   | 320   |
		| 398                   | 320   |
		| 172                   | 320   |
		| 173                   | 320   |
		| 171                   | 320   |
		| 404                   | 320   |
		| 402                   | 320   |
		| 176                   | 320   |
		| 177                   | 320   |
		| 175                   | 320   |
		| 408                   | 320   |
		| 406                   | 320   |
		| 180                   | 320   |
		| 181                   | 320   |
		| 179                   | 320   |
		| 412                   | 320   |
		| 410                   | 320   |
		| 184                   | 320   |
		| 185                   | 320   |
		| 183                   | 320   |
		| 416                   | 320   |
		| 414                   | 320   |
		| 212                   | 320   |
		| 213                   | 320   |
		| 211                   | 320   |
		| 422                   | 320   |
		| 420                   | 320   |
		| 215                   | 320   |
		| 216                   | 320   |
		| 214                   | 320   |
		| 425                   | 320   |
		| 423                   | 320   |
		| 191                   | 320   |
		| 192                   | 320   |
		| 196                   | 320   |
		| 197                   | 320   |
		| 194                   | 320   |
		| 190                   | 320   |
		| 193                   | 320   |
		| 189                   | 320   |
		| 207                   | 320   |
		| 209                   | 320   |
		| 210                   | 320   |
		| 202                   | 320   |
		| 203                   | 320   |
		| 204                   | 320   |
		| 208                   | 320   |
		| 201                   | 320   |
		| 188                   | 320   |
		| 199                   | 320   |
		| 200                   | 320   |
		| 187                   | 320   |
		| 419                   | 320   |
		| 417                   | 320   |
		| 564                   | 320   |
		| 217                   | 320   |
		| 428                   | 320   |
		| 426                   | 320   |
		| 220                   | 320   |
		| 221                   | 320   |
		| 219                   | 320   |
		| 565                   | 320   |
		| 218                   | 320   |
		| 431                   | 320   |
		| 429                   | 320   |
		| 84                    | 320   |
		| 85                    | 320   |
		| 83                    | 320   |
		| 228                   | 320   |
		| 236                   | 320   |
		| 223                   | 320   |
		| 235                   | 320   |
		| 511                   | 320   |
		| 512                   | 320   |
		| 241                   | 320   |
		| 239                   | 320   |
		| 240                   | 320   |
		| 226                   | 320   |
		| 225                   | 320   |
		| 560                   | 320   |
		| 227                   | 320   |
		| 434                   | 320   |
		| 432                   | 320   |
		| 229                   | 320   |
		| 438                   | 320   |
		| 436                   | 320   |
		| 230                   | 320   |
		| 442                   | 320   |
		| 440                   | 320   |
		| 231                   | 320   |
		| 446                   | 320   |
		| 444                   | 320   |
		| 232                   | 320   |
		| 451                   | 320   |
		| 448                   | 320   |
		| 233                   | 320   |
		| 454                   | 320   |
		| 452                   | 320   |
		| 234                   | 320   |
		| 458                   | 320   |
		| 456                   | 320   |
		| 520                   | 320   |
		| 521                   | 320   |
		| 522                   | 320   |
		| 523                   | 320   |
		| 524                   | 320   |
		| 712                   | 320   |
		| 519                   | 320   |
		| 525                   | 320   |
		| 518                   | 320   |
		| 526                   | 320   |
		| 527                   | 320   |
		| 693                   | 320   |
		| 694                   | 320   |
		| 517                   | 320   |
		| 528                   | 320   |
		| 530                   | 320   |
		| 533                   | 320   |
		| 534                   | 320   |
		| 516                   | 320   |
		| 529                   | 320   |
		| 515                   | 320   |
		| 692                   | 320   |
		| 541                   | 320   |
		| 540                   | 320   |
		| 544                   | 320   |
		| 539                   | 320   |
		| 542                   | 320   |
		| 543                   | 320   |
		| 713                   | 320   |
		| 538                   | 320   |
		| 537                   | 320   |
		| 545                   | 320   |
		| 536                   | 320   |
		| 535                   | 320   |
		| 554                   | 320   |
		| 555                   | 320   |
		| 556                   | 320   |
		| 552                   | 320   |
		| 553                   | 320   |
		| 558                   | 320   |
		| 550                   | 320   |
		| 557                   | 320   |
		| 547                   | 320   |
		| 548                   | 320   |
		| 514                   | 320   |
		| 546                   | 320   |
		| 513                   | 320   |
		| 562                   | 320   |
		| 466                   | 320   |
		| 467                   | 320   |
		| 732                   | 320   |
		| 567                   | 320   |
		| 465                   | 320   |
		| 654                   | 320   |
		| 469                   | 320   |
		| 470                   | 320   |
		| 468                   | 320   |
		| 656                   | 320   |
		| 477                   | 320   |
		| 478                   | 320   |
		| 476                   | 320   |
		| 658                   | 320   |
		| 730                   | 320   |
		| 464                   | 320   |
		| 662                   | 320   |
		| 659                   | 320   |
		| 459                   | 320   |
		| 460                   | 320   |
		| 461                   | 320   |
		| 462                   | 320   |
		| 566                   | 320   |
		| 568                   | 320   |
		| 570                   | 320   |
		| 571                   | 320   |
		| 572                   | 320   |
		| 573                   | 320   |
		| 575                   | 320   |
		| 576                   | 320   |
		| 577                   | 320   |
		| 583                   | 320   |
		| 585                   | 320   |
		| 587                   | 320   |
		| 588                   | 320   |
		| 590                   | 320   |
		| 592                   | 320   |
		| 593                   | 320   |
		| 596                   | 320   |
		| 598                   | 320   |
		| 601                   | 320   |
		| 616                   | 320   |
		| 620                   | 320   |
		| 621                   | 320   |
		| 623                   | 320   |
		| 625                   | 320   |
		| 628                   | 320   |
		| 630                   | 320   |
		| 632                   | 320   |
		| 634                   | 320   |
		| 636                   | 320   |
		| 579                   | 320   |
		| 581                   | 320   |
		| 603                   | 320   |
		| 614                   | 320   |
		| 633                   | 320   |
		| 638                   | 320   |
		| 645                   | 320   |
		| 688                   | 320   |
		| 643                   | 320   |
		| 686                   | 320   |
		| 723                   | 320   |
		| 725                   | 320   |
		| 721                   | 320   |
		| 726                   | 320   |
		| 222                   | 320   |
		| 641                   | 320   |
		| 647                   | 320   |
		| 649                   | 320   |
		| 651                   | 320   |
		| 674                   | 320   |
		| 714                   | 320   |
		| 716                   | 320   |
		| 715                   | 320   |
		| 683                   | 320   |
		| 640                   | 320   |
		| 727                   | 320   |
		| 720                   | 320   |
		| 696                   | 320   |
		| 698                   | 320   |
		| 702                   | 320   |
		| 701                   | 320   |
		| 695                   | 320   |
		| 699                   | 320   |
		| 700                   | 320   |
		| 731                   | 320   |
		| 610                   | 320   |
		| 611                   | 320   |
		| 607                   | 320   |
		| 608                   | 320   |
		| 609                   | 320   |
		| 606                   | 320   |
		| 613                   | 320   |
		| 463                   | 320   |
		| 604                   | 320   |
		| 728                   | 320   |
		| 706                   | 320   |
		| 707                   | 320   |
		| 661                   | 320   |
		| 729                   | 320   |
		| 709                   | 320   |
		| 719                   | 320   |
		| 710                   | 320   |
		| 711                   | 320   |
		| 738                   | 320   |
		| 739                   | 320   |
		| 735                   | 320   |
		| 734                   | 320   |
		| 733                   | 320   |
		| 737                   | 320   |
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
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000002           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Draft             |
		| TotalFunding    | 12000             |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name                                                                                            | FundingLineCode | Value | TemplateLineId | Type        |
		| SBS Exceptional Factors                                                                         | GAG-004         | 640   | 49             | Payment     |
		| Post Opening Grant - Leadership Diseconomies                                                    | GAG-007         | 0     | 303            | Payment     |
		| Post Opening Grant - Per Pupil Resources                                                        | GAG-006         | 0     | 302            | Payment     |
		| Allocation protection                                                                           | GAG-012         | 320   | 300            | Payment     |
		| De-Delegated funding retained by the LA                                                         | GAG-014         | 320   | 705            | Payment     |
		| SBS Other Factors                                                                               | GAG-003         | 640   | 40             | Payment     |
		| Start up Grant Part A                                                                           | GAG-008         | 320   | 304            | Payment     |
		| Start up Grant Part B                                                                           | GAG-009         | 1600  | 305            | Payment     |
		| Hospital Provision                                                                              | GAG-010         | 1280  | 306            | Payment     |
		| Pre-16 High Needs funding                                                                       | GAG-011         | 14720 | 307            | Payment     |
		| Minimum Funding Guarantee                                                                       | GAG-005         | 640   | 297            | Payment     |
		| SBS Pupil Led Factors                                                                           | GAG-001         | 3840  | 2              | Payment     |
		| PFI Front Loaded                                                                                | GAG-002         | 15360 | 39             | Payment     |
		| School Allocation Block With Notional SEN And DeDelegation                                      |                 | 0     | 0              | Information |
		| School Budget Share                                                                             |                 | 0     | 1              | Information |
		| Primary IDACI Band C Funding                                                                    |                 | 0     | 10             | Information |
		| Primary IDACI Band D Funding                                                                    |                 | 0     | 11             | Information |
		| Primary IDACI Band E Funding                                                                    |                 | 0     | 12             | Information |
		| Primary IDACI Band F Funding                                                                    |                 | 0     | 13             | Information |
		| Secondary IDACI Band A Funding                                                                  |                 | 0     | 14             | Information |
		| Secondary IDACI Band B Funding                                                                  |                 | 0     | 15             | Information |
		| Secondary IDACI Band C Funding                                                                  |                 | 0     | 16             | Information |
		| Secondary IDACI Band D Funding                                                                  |                 | 0     | 17             | Information |
		| Secondary IDACI Band E Funding                                                                  |                 | 0     | 18             | Information |
		| Secondary IDACI Band F Funding                                                                  |                 | 0     | 19             | Information |
		| Primary Free School Meals FSM Funding                                                           |                 | 0     | 20             | Information |
		| Primary FSM6 Funding                                                                            |                 | 0     | 21             | Information |
		| Secondary Free School Meals FSM Funding                                                         |                 | 0     | 22             | Information |
		| Secondary FSM6 Funding                                                                          |                 | 0     | 23             | Information |
		| Looked After Children LA C Funding                                                              |                 | 0     | 24             | Information |
		| School Allocation Block                                                                         |                 | 0     | 242            | Information |
		| Notional SEN Funding                                                                            |                 | 0     | 243            | Information |
		| Basic Entitlement Age Weighted Pupil SEN                                                        |                 | 0     | 244            | Information |
		| Basic Entitlement Primary Including Reception SEN                                               |                 | 0     | 245            | Information |
		| Basic Entitlement KS3 SEN                                                                       |                 | 0     | 246            | Information |
		| Basic Entitlement KS4 SEN                                                                       |                 | 0     | 247            | Information |
		| Deprivation SEN                                                                                 |                 | 0     | 248            | Information |
		| Primary IDACI Band A SEN                                                                        |                 | 0     | 249            | Information |
		| Prior Attainment                                                                                |                 | 0     | 25             | Information |
		| Primary IDACI Band B SEN                                                                        |                 | 0     | 250            | Information |
		| Primary IDACI Band C SEN                                                                        |                 | 0     | 251            | Information |
		| Primary IDACI Band D SEN                                                                        |                 | 0     | 253            | Information |
		| Primary IDACI Band E SEN                                                                        |                 | 0     | 254            | Information |
		| Primary IDACI Band F SEN                                                                        |                 | 0     | 255            | Information |
		| Secondary IDACI Band A SEN                                                                      |                 | 0     | 256            | Information |
		| Secondary IDACI Band B SEN                                                                      |                 | 0     | 257            | Information |
		| Secondary IDACI Band C SEN                                                                      |                 | 0     | 258            | Information |
		| Secondary IDACI Band D SEN                                                                      |                 | 0     | 259            | Information |
		| Primary Attainment Low Primary Prior Attainment Funding                                         |                 | 0     | 26             | Information |
		| Secondary IDACIBand E SEN                                                                       |                 | 0     | 260            | Information |
		| Secondary IDACIBand F SEN                                                                       |                 | 0     | 261            | Information |
		| Primary Free School Meals FSM SEN                                                               |                 | 0     | 262            | Information |
		| Primary FSM6 SEN                                                                                |                 | 0     | 263            | Information |
		| Secondary Free School Meals FSM SEN                                                             |                 | 0     | 264            | Information |
		| Pupil Led Factors SEN                                                                           |                 | 0     | 265            | Information |
		| Other Factors SEN                                                                               |                 | 0     | 266            | Information |
		| Exceptional Factors SEN                                                                         |                 | 0     | 267            | Information |
		| MFG SEN                                                                                         |                 | 0     | 268            | Information |
		| Secondary FSM6 SEN                                                                              |                 | 0     | 269            | Information |
		| Secondary Attainment Secondary Pupils Not Achieving The Expected Standards In KS2 Tests Funding |                 | 0     | 27             | Information |
		| Prior Attainment SEN                                                                            |                 | 0     | 270            | Information |
		| Primary Attainment LowPrimaryPriorAttainmentSEN                                                 |                 | 0     | 271            | Information |
		| Secondary Attainment Secondary Pupils Not Achieving The Expected Standards In KS2 Tests SEN     |                 | 0     | 272            | Information |
		| English As An Additional Language EAL SEN                                                       |                 | 0     | 273            | Information |
		| Primary EAL Band 1 SEN                                                                          |                 | 0     | 274            | Information |
		| Primary EAL Band 2 SEN                                                                          |                 | 0     | 275            | Information |
		| Primary EAL Band 3 SEN                                                                          |                 | 0     | 276            | Information |
		| Secondary EAL Band 1 SEN                                                                        |                 | 0     | 277            | Information |
		| Secondary EAL Band 2 SEN                                                                        |                 | 0     | 278            | Information |
		| Secondary EAL Band 3 SEN                                                                        |                 | 0     | 279            | Information |
		| English As An Additional Language EAL Funding                                                   |                 | 0     | 28             | Information |
		| Mobility SEN                                                                                    |                 | 0     | 280            | Information |
		| Primary Mobility SEN                                                                            |                 | 0     | 281            | Information |
		| Secondary Mobility SEN                                                                          |                 | 0     | 282            | Information |
		| Sparsity SEN                                                                                    |                 | 0     | 283            | Information |
		| Lump Sum SEN                                                                                    |                 | 0     | 284            | Information |
		| Primary Lump Sum SEN                                                                            |                 | 0     | 285            | Information |
		| Secondary Lump Sum SEN                                                                          |                 | 0     | 286            | Information |
		| Split Sites SEN                                                                                 |                 | 0     | 287            | Information |
		| Standard PFI SEN                                                                                |                 | 0     | 288            | Information |
		| MFL SEN                                                                                         |                 | 0     | 289            | Information |
		| Primary EAL Band 1 Funding                                                                      |                 | 0     | 29             | Information |
		| Exceptional Circumstance 1 SEN                                                                  |                 | 0     | 290            | Information |
		| Exceptional Circumstance 2 SEN                                                                  |                 | 0     | 291            | Information |
		| Exceptional Circumstance 3 SEN                                                                  |                 | 0     | 292            | Information |
		| Exceptional Circumstance 4 SEN                                                                  |                 | 0     | 293            | Information |
		| Exceptional Circumstance 5 SEN                                                                  |                 | 0     | 294            | Information |
		| Exceptional Circumstance 6 SEN                                                                  |                 | 0     | 295            | Information |
		| Exceptional Circumstance 7 SEN                                                                  |                 | 0     | 296            | Information |
		| Total Post Opening Grant Start Up Grant Allocation                                              |                 | 0     | 298            | Information |
		| Total High Needs Allocation                                                                     |                 | 0     | 299            | Information |
		| Basic Entitlement Age Weighted Pupil Unit                                                       |                 | 0     | 3              | Information |
		| Primary EAL Band 2 Funding                                                                      |                 | 0     | 30             | Information |
		| Special Unoccupied                                                                              |                 | 0     | 308            | Information |
		| Special Occupied                                                                                |                 | 0     | 309            | Information |
		| Primary EAL Band 3 Funding                                                                      |                 | 0     | 31             | Information |
		| Alternative Provision                                                                           |                 | 0     | 310            | Information |
		| Secondary EAL Band 1 Funding                                                                    |                 | 0     | 32             | Information |
		| Secondary EAL Band 2 Funding                                                                    |                 | 0     | 33             | Information |
		| Looked After Children LAC SEN                                                                   |                 | 0     | 337            | Information |
		| Secondary EALBand3Funding                                                                       |                 | 0     | 34             | Information |
		| Mobility Funding                                                                                |                 | 0     | 35             | Information |
		| Primary Mobility Funding                                                                        |                 | 0     | 36             | Information |
		| Secondary Mobility Funding                                                                      |                 | 0     | 37             | Information |
		| SBS Other Factors Summary                                                                       |                 | 0     | 38             | Information |
		| Basic Entitlement Primary Funding                                                               |                 | 0     | 4              | Information |
		| Sparsity Funding                                                                                |                 | 0     | 41             | Information |
		| Lump Sum                                                                                        |                 | 0     | 42             | Information |
		| Primary Lump Sum                                                                                |                 | 0     | 43             | Information |
		| Secondary Lump Sum                                                                              |                 | 0     | 44             | Information |
		| SplitSite                                                                                       |                 | 0     | 45             | Information |
		| PFI                                                                                             |                 | 0     | 46             | Information |
		| London Fringe                                                                                   |                 | 0     | 47             | Information |
		| MFL Adjustment                                                                                  |                 | 0     | 48             | Information |
		| Basic Entitlement KS3 Funding                                                                   |                 | 0     | 5              | Information |
		| Exceptional Circumstance 1 Funding                                                              |                 | 0     | 50             | Information |
		| Exceptional Circumstance 2 Funding                                                              |                 | 0     | 51             | Information |
		| Exceptional Circumstance 3 Funding                                                              |                 | 0     | 52             | Information |
		| Exceptional Circumstance 4 Funding                                                              |                 | 0     | 53             | Information |
		| Exceptional Circumstance 5 Funding                                                              |                 | 0     | 54             | Information |
		| Exceptional Circumstance 6 Funding                                                              |                 | 0     | 55             | Information |
		| Exceptional Circumstance 7 Funding                                                              |                 | 0     | 56             | Information |
		| Prior Year Adjustment To SBS                                                                    |                 | 0     | 57             | Information |
		| Basic Entitlement KS4 Funding                                                                   |                 | 0     | 6              | Information |
		| Deprivation                                                                                     |                 | 0     | 7              | Information |
		| Funding Previously De Delegated                                                                 |                 | 0     | 704            | Information |
		| Total School Allocation With High Needs                                                         |                 | 0     | 718            | Information |
		| Primary IDACI Band A Funding                                                                    |                 | 0     | 8              | Information |
		| Primary IDACI Band B Funding                                                                    |                 | 0     | 9              | Information |
	And the Published Provider has the following distribution period for funding line 'GAG-002'
		| DistributionPeriodId | Value |
		| AC-1920              | 7000  |
		| AC-2021              | 5000  |
	And the Published Providers distribution period has the following profiles for funding line 'GAG-002'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AC-1920              | CalendarMonth | October   | 1920 | 1          | 7000          |
		| AC-2021              | CalendarMonth | April     | 2021 | 1          | 5000          |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 703                   | 320   |
		| 59                    | 320   |
		| 60                    | 320   |
		| 61                    | 320   |
		| 238                   | 320   |
		| 58                    | 320   |
		| 314                   | 320   |
		| 237                   | 320   |
		| 312                   | 320   |
		| 63                    | 320   |
		| 64                    | 320   |
		| 66                    | 320   |
		| 62                    | 320   |
		| 317                   | 320   |
		| 315                   | 320   |
		| 68                    | 320   |
		| 69                    | 320   |
		| 70                    | 320   |
		| 67                    | 320   |
		| 321                   | 320   |
		| 319                   | 320   |
		| 72                    | 320   |
		| 73                    | 320   |
		| 71                    | 320   |
		| 324                   | 320   |
		| 322                   | 320   |
		| 76                    | 320   |
		| 77                    | 320   |
		| 75                    | 320   |
		| 327                   | 320   |
		| 325                   | 320   |
		| 80                    | 320   |
		| 81                    | 320   |
		| 79                    | 320   |
		| 330                   | 320   |
		| 328                   | 320   |
		| 340                   | 320   |
		| 338                   | 320   |
		| 88                    | 320   |
		| 89                    | 320   |
		| 87                    | 320   |
		| 343                   | 320   |
		| 341                   | 320   |
		| 92                    | 320   |
		| 93                    | 320   |
		| 91                    | 320   |
		| 346                   | 320   |
		| 344                   | 320   |
		| 96                    | 320   |
		| 97                    | 320   |
		| 98                    | 320   |
		| 95                    | 320   |
		| 349                   | 320   |
		| 347                   | 320   |
		| 100                   | 320   |
		| 101                   | 320   |
		| 99                    | 320   |
		| 352                   | 320   |
		| 350                   | 320   |
		| 104                   | 320   |
		| 105                   | 320   |
		| 103                   | 320   |
		| 356                   | 320   |
		| 354                   | 320   |
		| 108                   | 320   |
		| 109                   | 320   |
		| 107                   | 320   |
		| 359                   | 320   |
		| 357                   | 320   |
		| 112                   | 320   |
		| 113                   | 320   |
		| 111                   | 320   |
		| 362                   | 320   |
		| 360                   | 320   |
		| 116                   | 320   |
		| 117                   | 320   |
		| 115                   | 320   |
		| 365                   | 320   |
		| 363                   | 320   |
		| 120                   | 320   |
		| 121                   | 320   |
		| 119                   | 320   |
		| 368                   | 320   |
		| 366                   | 320   |
		| 124                   | 320   |
		| 125                   | 320   |
		| 123                   | 320   |
		| 371                   | 320   |
		| 369                   | 320   |
		| 130                   | 320   |
		| 131                   | 320   |
		| 129                   | 320   |
		| 374                   | 320   |
		| 372                   | 320   |
		| 135                   | 320   |
		| 136                   | 320   |
		| 134                   | 320   |
		| 377                   | 320   |
		| 375                   | 320   |
		| 139                   | 320   |
		| 140                   | 320   |
		| 141                   | 320   |
		| 138                   | 320   |
		| 380                   | 320   |
		| 378                   | 320   |
		| 145                   | 320   |
		| 146                   | 320   |
		| 144                   | 320   |
		| 383                   | 320   |
		| 381                   | 320   |
		| 150                   | 320   |
		| 151                   | 320   |
		| 149                   | 320   |
		| 386                   | 320   |
		| 384                   | 320   |
		| 154                   | 320   |
		| 155                   | 320   |
		| 153                   | 320   |
		| 389                   | 320   |
		| 387                   | 320   |
		| 159                   | 320   |
		| 160                   | 320   |
		| 158                   | 320   |
		| 392                   | 320   |
		| 390                   | 320   |
		| 163                   | 320   |
		| 164                   | 320   |
		| 162                   | 320   |
		| 396                   | 320   |
		| 394                   | 320   |
		| 169                   | 320   |
		| 168                   | 320   |
		| 167                   | 320   |
		| 401                   | 320   |
		| 398                   | 320   |
		| 172                   | 320   |
		| 173                   | 320   |
		| 171                   | 320   |
		| 404                   | 320   |
		| 402                   | 320   |
		| 176                   | 320   |
		| 177                   | 320   |
		| 175                   | 320   |
		| 408                   | 320   |
		| 406                   | 320   |
		| 180                   | 320   |
		| 181                   | 320   |
		| 179                   | 320   |
		| 412                   | 320   |
		| 410                   | 320   |
		| 184                   | 320   |
		| 185                   | 320   |
		| 183                   | 320   |
		| 416                   | 320   |
		| 414                   | 320   |
		| 212                   | 320   |
		| 213                   | 320   |
		| 211                   | 320   |
		| 422                   | 320   |
		| 420                   | 320   |
		| 215                   | 320   |
		| 216                   | 320   |
		| 214                   | 320   |
		| 425                   | 320   |
		| 423                   | 320   |
		| 191                   | 320   |
		| 192                   | 320   |
		| 196                   | 320   |
		| 197                   | 320   |
		| 194                   | 320   |
		| 190                   | 320   |
		| 193                   | 320   |
		| 189                   | 320   |
		| 207                   | 320   |
		| 209                   | 320   |
		| 210                   | 320   |
		| 202                   | 320   |
		| 203                   | 320   |
		| 204                   | 320   |
		| 208                   | 320   |
		| 201                   | 320   |
		| 188                   | 320   |
		| 199                   | 320   |
		| 200                   | 320   |
		| 187                   | 320   |
		| 419                   | 320   |
		| 417                   | 320   |
		| 564                   | 320   |
		| 217                   | 320   |
		| 428                   | 320   |
		| 426                   | 320   |
		| 220                   | 320   |
		| 221                   | 320   |
		| 219                   | 320   |
		| 565                   | 320   |
		| 218                   | 320   |
		| 431                   | 320   |
		| 429                   | 320   |
		| 84                    | 320   |
		| 85                    | 320   |
		| 83                    | 320   |
		| 228                   | 320   |
		| 236                   | 320   |
		| 223                   | 320   |
		| 235                   | 320   |
		| 511                   | 320   |
		| 512                   | 320   |
		| 241                   | 320   |
		| 239                   | 320   |
		| 240                   | 320   |
		| 226                   | 320   |
		| 225                   | 320   |
		| 560                   | 320   |
		| 227                   | 320   |
		| 434                   | 320   |
		| 432                   | 320   |
		| 229                   | 320   |
		| 438                   | 320   |
		| 436                   | 320   |
		| 230                   | 320   |
		| 442                   | 320   |
		| 440                   | 320   |
		| 231                   | 320   |
		| 446                   | 320   |
		| 444                   | 320   |
		| 232                   | 320   |
		| 451                   | 320   |
		| 448                   | 320   |
		| 233                   | 320   |
		| 454                   | 320   |
		| 452                   | 320   |
		| 234                   | 320   |
		| 458                   | 320   |
		| 456                   | 320   |
		| 520                   | 320   |
		| 521                   | 320   |
		| 522                   | 320   |
		| 523                   | 320   |
		| 524                   | 320   |
		| 712                   | 320   |
		| 519                   | 320   |
		| 525                   | 320   |
		| 518                   | 320   |
		| 526                   | 320   |
		| 527                   | 320   |
		| 693                   | 320   |
		| 694                   | 320   |
		| 517                   | 320   |
		| 528                   | 320   |
		| 530                   | 320   |
		| 533                   | 320   |
		| 534                   | 320   |
		| 516                   | 320   |
		| 529                   | 320   |
		| 515                   | 320   |
		| 692                   | 320   |
		| 541                   | 320   |
		| 540                   | 320   |
		| 544                   | 320   |
		| 539                   | 320   |
		| 542                   | 320   |
		| 543                   | 320   |
		| 713                   | 320   |
		| 538                   | 320   |
		| 537                   | 320   |
		| 545                   | 320   |
		| 536                   | 320   |
		| 535                   | 320   |
		| 554                   | 320   |
		| 555                   | 320   |
		| 556                   | 320   |
		| 552                   | 320   |
		| 553                   | 320   |
		| 558                   | 320   |
		| 550                   | 320   |
		| 557                   | 320   |
		| 547                   | 320   |
		| 548                   | 320   |
		| 514                   | 320   |
		| 546                   | 320   |
		| 513                   | 320   |
		| 562                   | 320   |
		| 466                   | 320   |
		| 467                   | 320   |
		| 732                   | 320   |
		| 567                   | 320   |
		| 465                   | 320   |
		| 654                   | 320   |
		| 469                   | 320   |
		| 470                   | 320   |
		| 468                   | 320   |
		| 656                   | 320   |
		| 477                   | 320   |
		| 478                   | 320   |
		| 476                   | 320   |
		| 658                   | 320   |
		| 730                   | 320   |
		| 464                   | 320   |
		| 662                   | 320   |
		| 659                   | 320   |
		| 459                   | 320   |
		| 460                   | 320   |
		| 461                   | 320   |
		| 462                   | 320   |
		| 566                   | 320   |
		| 568                   | 320   |
		| 570                   | 320   |
		| 571                   | 320   |
		| 572                   | 320   |
		| 573                   | 320   |
		| 575                   | 320   |
		| 576                   | 320   |
		| 577                   | 320   |
		| 583                   | 320   |
		| 585                   | 320   |
		| 587                   | 320   |
		| 588                   | 320   |
		| 590                   | 320   |
		| 592                   | 320   |
		| 593                   | 320   |
		| 596                   | 320   |
		| 598                   | 320   |
		| 601                   | 320   |
		| 616                   | 320   |
		| 620                   | 320   |
		| 621                   | 320   |
		| 623                   | 320   |
		| 625                   | 320   |
		| 628                   | 320   |
		| 630                   | 320   |
		| 632                   | 320   |
		| 634                   | 320   |
		| 636                   | 320   |
		| 579                   | 320   |
		| 581                   | 320   |
		| 603                   | 320   |
		| 614                   | 320   |
		| 633                   | 320   |
		| 638                   | 320   |
		| 645                   | 320   |
		| 688                   | 320   |
		| 643                   | 320   |
		| 686                   | 320   |
		| 723                   | 320   |
		| 725                   | 320   |
		| 721                   | 320   |
		| 726                   | 320   |
		| 222                   | 320   |
		| 641                   | 320   |
		| 647                   | 320   |
		| 649                   | 320   |
		| 651                   | 320   |
		| 674                   | 320   |
		| 714                   | 320   |
		| 716                   | 320   |
		| 715                   | 320   |
		| 683                   | 320   |
		| 640                   | 320   |
		| 727                   | 320   |
		| 720                   | 320   |
		| 696                   | 320   |
		| 698                   | 320   |
		| 702                   | 320   |
		| 701                   | 320   |
		| 695                   | 320   |
		| 699                   | 320   |
		| 700                   | 320   |
		| 731                   | 320   |
		| 610                   | 320   |
		| 611                   | 320   |
		| 607                   | 320   |
		| 608                   | 320   |
		| 609                   | 320   |
		| 606                   | 320   |
		| 613                   | 320   |
		| 463                   | 320   |
		| 604                   | 320   |
		| 728                   | 320   |
		| 706                   | 320   |
		| 707                   | 320   |
		| 661                   | 320   |
		| 729                   | 320   |
		| 709                   | 320   |
		| 719                   | 320   |
		| 710                   | 320   |
		| 711                   | 320   |
		| 738                   | 320   |
		| 739                   | 320   |
		| 735                   | 320   |
		| 734                   | 320   |
		| 733                   | 320   |
		| 737                   | 320   |
	And the Published Provider has the following provider information
		| Field              | Value                    |
		| ProviderId         | 1000002                  |
		| Name               | Maintained School 2      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Maintained School 2      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000002                  |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 1000002                  |
		| Name               | Maintained School 2      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Maintained School 2      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000002                  |
	And the provider with id '1000002' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000003           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Draft             |
		| TotalFunding    | 12000             |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name                                         | FundingLineCode | Value | TemplateLineId | Type    |
		| SBS Exceptional Factors                      | GAG-004         | 640   | 5              | Payment |
		| Post Opening Grant - Leadership Diseconomies | GAG-007         | 0     | 6              | Payment |
		| Post Opening Grant - Per Pupil Resources     | GAG-006         | 0     | 7              | Payment |
		| Allocation protection                        | GAG-012         | 320   | 23             | Payment |
		| Funding for Historic Commitments             | GAG-013         | 0     | 27             | Payment |
		| De-Delegated funding retained by the LA      | GAG-014         | 320   | 130            | Payment |
		| SBS Other Factors                            | GAG-003         | 640   | 4              | Payment |
		| Start up Grant Part A                        | GAG-008         | 320   | 8              | Payment |
		| Start up Grant Part B                        | GAG-009         | 1600  | 9              | Payment |
		| Hospital Provision                           | GAG-010         | 1280  | 10             | Payment |
		| Pre-16 High Needs funding                    | GAG-011         | 14720 | 11             | Payment |
		| Minimum Funding Guarantee                    | GAG-005         | 640   | 12             | Payment |
		| SBS Pupil Led Factors                        | GAG-001         | 3840  | 2              | Payment |
		| PFI Front Loaded                             | GAG-002         | 15360 | 3              | Payment |
	And the Published Provider has the following distribution period for funding line 'GAG-002'
		| DistributionPeriodId | Value |
		| AC-1920              | 7000  |
		| AC-2021              | 5000  |
	And the Published Providers distribution period has the following profiles for funding line 'GAG-002'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AC-1920              | CalendarMonth | October   | 1920 | 1          | 7000          |
		| AC-2021              | CalendarMonth | April     | 2021 | 1          | 5000          |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 703                   |       |
		| 59                    |       |
		| 60                    |       |
		| 61                    |       |
		| 238                   |       |
		| 58                    |       |
		| 314                   |       |
		| 237                   |       |
		| 312                   |       |
		| 63                    |       |
		| 64                    |       |
		| 66                    |       |
		| 62                    |       |
		| 317                   |       |
		| 315                   |       |
		| 68                    |       |
		| 69                    |       |
		| 70                    |       |
		| 67                    |       |
		| 321                   |       |
		| 319                   |       |
		| 72                    |       |
		| 73                    |       |
		| 71                    |       |
		| 324                   |       |
		| 322                   |       |
		| 76                    |       |
		| 77                    |       |
		| 75                    |       |
		| 327                   |       |
		| 325                   |       |
		| 80                    |       |
		| 81                    |       |
		| 79                    |       |
		| 330                   |       |
		| 328                   |       |
		| 340                   |       |
		| 338                   |       |
		| 88                    |       |
		| 89                    |       |
		| 87                    |       |
		| 343                   |       |
		| 341                   |       |
		| 92                    |       |
		| 93                    |       |
		| 91                    |       |
		| 346                   |       |
		| 344                   |       |
		| 96                    |       |
		| 97                    |       |
		| 98                    |       |
		| 95                    |       |
		| 349                   |       |
		| 347                   |       |
		| 100                   |       |
		| 101                   |       |
		| 99                    |       |
		| 352                   |       |
		| 350                   |       |
		| 104                   |       |
		| 105                   |       |
		| 103                   |       |
		| 356                   |       |
		| 354                   |       |
		| 108                   |       |
		| 109                   |       |
		| 107                   |       |
		| 359                   |       |
		| 357                   |       |
		| 112                   |       |
		| 113                   |       |
		| 111                   |       |
		| 362                   |       |
		| 360                   |       |
		| 116                   |       |
		| 117                   |       |
		| 115                   |       |
		| 365                   |       |
		| 363                   |       |
		| 120                   |       |
		| 121                   |       |
		| 119                   |       |
		| 368                   |       |
		| 366                   |       |
		| 124                   |       |
		| 125                   |       |
		| 123                   |       |
		| 371                   |       |
		| 369                   |       |
		| 130                   |       |
		| 131                   |       |
		| 129                   |       |
		| 374                   |       |
		| 372                   |       |
		| 135                   |       |
		| 136                   |       |
		| 134                   |       |
		| 377                   |       |
		| 375                   |       |
		| 139                   |       |
		| 140                   |       |
		| 141                   |       |
		| 138                   |       |
		| 380                   |       |
		| 378                   |       |
		| 145                   |       |
		| 146                   |       |
		| 144                   |       |
		| 383                   |       |
		| 381                   |       |
		| 150                   |       |
		| 151                   |       |
		| 149                   |       |
		| 386                   |       |
		| 384                   |       |
		| 154                   |       |
		| 155                   |       |
		| 153                   |       |
		| 389                   |       |
		| 387                   |       |
		| 159                   |       |
		| 160                   |       |
		| 158                   |       |
		| 392                   |       |
		| 390                   |       |
		| 163                   |       |
		| 164                   |       |
		| 162                   |       |
		| 396                   |       |
		| 394                   |       |
		| 169                   |       |
		| 168                   |       |
		| 167                   |       |
		| 401                   |       |
		| 398                   |       |
		| 172                   |       |
		| 173                   |       |
		| 171                   |       |
		| 404                   |       |
		| 402                   |       |
		| 176                   |       |
		| 177                   |       |
		| 175                   |       |
		| 408                   |       |
		| 406                   |       |
		| 180                   |       |
		| 181                   |       |
		| 179                   |       |
		| 412                   |       |
		| 410                   |       |
		| 184                   |       |
		| 185                   |       |
		| 183                   |       |
		| 416                   |       |
		| 414                   |       |
		| 212                   |       |
		| 213                   |       |
		| 211                   |       |
		| 422                   |       |
		| 420                   |       |
		| 215                   |       |
		| 216                   |       |
		| 214                   |       |
		| 425                   |       |
		| 423                   |       |
		| 191                   |       |
		| 192                   |       |
		| 196                   |       |
		| 197                   |       |
		| 194                   |       |
		| 190                   |       |
		| 193                   |       |
		| 189                   |       |
		| 207                   |       |
		| 209                   |       |
		| 210                   |       |
		| 202                   |       |
		| 203                   |       |
		| 204                   |       |
		| 208                   |       |
		| 201                   |       |
		| 188                   |       |
		| 199                   |       |
		| 200                   |       |
		| 187                   |       |
		| 419                   |       |
		| 417                   |       |
		| 564                   |       |
		| 217                   |       |
		| 428                   |       |
		| 426                   |       |
		| 220                   |       |
		| 221                   |       |
		| 219                   |       |
		| 565                   |       |
		| 218                   |       |
		| 431                   |       |
		| 429                   |       |
		| 84                    |       |
		| 85                    |       |
		| 83                    |       |
		| 228                   |       |
		| 236                   |       |
		| 223                   |       |
		| 235                   |       |
		| 511                   |       |
		| 512                   |       |
		| 241                   |       |
		| 239                   |       |
		| 240                   |       |
		| 226                   |       |
		| 225                   |       |
		| 560                   |       |
		| 227                   |       |
		| 434                   |       |
		| 432                   |       |
		| 229                   |       |
		| 438                   |       |
		| 436                   |       |
		| 230                   |       |
		| 442                   |       |
		| 440                   |       |
		| 231                   |       |
		| 446                   |       |
		| 444                   |       |
		| 232                   |       |
		| 451                   |       |
		| 448                   |       |
		| 233                   |       |
		| 454                   |       |
		| 452                   |       |
		| 234                   |       |
		| 458                   |       |
		| 456                   |       |
		| 520                   |       |
		| 521                   |       |
		| 522                   |       |
		| 523                   |       |
		| 524                   |       |
		| 712                   |       |
		| 519                   |       |
		| 525                   |       |
		| 518                   |       |
		| 526                   |       |
		| 527                   |       |
		| 693                   |       |
		| 694                   |       |
		| 517                   |       |
		| 528                   |       |
		| 530                   |       |
		| 533                   |       |
		| 534                   |       |
		| 516                   |       |
		| 529                   |       |
		| 515                   |       |
		| 692                   |       |
		| 541                   |       |
		| 540                   |       |
		| 544                   |       |
		| 539                   |       |
		| 542                   |       |
		| 543                   |       |
		| 713                   |       |
		| 538                   |       |
		| 537                   |       |
		| 545                   |       |
		| 536                   |       |
		| 535                   |       |
		| 554                   |       |
		| 555                   |       |
		| 556                   |       |
		| 552                   |       |
		| 553                   |       |
		| 558                   |       |
		| 550                   |       |
		| 557                   |       |
		| 547                   |       |
		| 548                   |       |
		| 514                   |       |
		| 546                   |       |
		| 513                   |       |
		| 562                   |       |
		| 466                   |       |
		| 467                   |       |
		| 732                   |       |
		| 567                   |       |
		| 465                   |       |
		| 654                   |       |
		| 469                   |       |
		| 470                   |       |
		| 468                   |       |
		| 656                   |       |
		| 477                   |       |
		| 478                   |       |
		| 476                   |       |
		| 658                   |       |
		| 730                   |       |
		| 464                   |       |
		| 662                   |       |
		| 659                   |       |
		| 459                   |       |
		| 460                   |       |
		| 461                   |       |
		| 462                   |       |
		| 566                   |       |
		| 568                   |       |
		| 570                   |       |
		| 571                   |       |
		| 572                   |       |
		| 573                   |       |
		| 575                   |       |
		| 576                   |       |
		| 577                   |       |
		| 583                   |       |
		| 585                   |       |
		| 587                   |       |
		| 588                   |       |
		| 590                   |       |
		| 592                   |       |
		| 593                   |       |
		| 596                   |       |
		| 598                   |       |
		| 601                   |       |
		| 616                   |       |
		| 620                   |       |
		| 621                   |       |
		| 623                   |       |
		| 625                   |       |
		| 628                   |       |
		| 630                   |       |
		| 632                   |       |
		| 634                   |       |
		| 636                   |       |
		| 579                   |       |
		| 581                   |       |
		| 603                   |       |
		| 614                   |       |
		| 633                   |       |
		| 638                   |       |
		| 645                   |       |
		| 688                   |       |
		| 643                   |       |
		| 686                   |       |
		| 723                   |       |
		| 725                   |       |
		| 721                   |       |
		| 726                   |       |
		| 222                   |       |
		| 641                   |       |
		| 647                   |       |
		| 649                   |       |
		| 651                   |       |
		| 674                   |       |
		| 714                   |       |
		| 716                   |       |
		| 715                   |       |
		| 683                   |       |
		| 640                   |       |
		| 727                   |       |
		| 720                   |       |
		| 696                   |       |
		| 698                   |       |
		| 702                   |       |
		| 701                   |       |
		| 695                   |       |
		| 699                   |       |
		| 700                   |       |
		| 731                   |       |
		| 610                   |       |
		| 611                   |       |
		| 607                   |       |
		| 608                   |       |
		| 609                   |       |
		| 606                   |       |
		| 613                   |       |
		| 463                   |       |
		| 604                   |       |
		| 728                   |       |
		| 706                   |       |
		| 707                   |       |
		| 661                   |       |
		| 729                   |       |
		| 709                   |       |
		| 719                   |       |
		| 710                   |       |
		| 711                   |       |
		| 738                   |       |
		| 739                   |       |
		| 735                   |       |
		| 734                   |       |
		| 733                   |       |
		| 737                   |       |
	And the Published Provider has the following provider information
		| Field              | Value                    |
		| ProviderId         | 1000003                  |
		| Name               | Maintained School 2      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Maintained School 2      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000003                  |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field              | Value                    |
		| ProviderId         | 1000003                  |
		| Name               | Maintained School 2      |
		| Authority          | Local Authority 1        |
		| DateOpened         | 2012-03-15               |
		| LACode             | 200                      |
		| LocalAuthorityName | Maintained School 2      |
		| ProviderType       | LA maintained schools    |
		| ProviderSubType    | Community school         |
		| ProviderVersionId  | <ProviderVersionId>      |
		| TrustStatus        | Not Supported By A Trust |
		| UKPRN              | 1000003                  |
	And the provider with id '1000003' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
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
	And calculation meta data exists for '<FundingStreamId>'
		| CalculationType | CalculationId                        | Name                                                                                                | PublishStatus |
		| Template        | 9ed150c9-072d-4c32-be8a-d0ce83e2dd1a | Reception Uplift pupil numbers                                                                      | Approved      |
		| Template        | 4f6058c6-07e8-4556-8c79-ec703ec55ee7 | Primary Basic Entitlement Rate                                                                      | Approved      |
		| Template        | 6c7c925a-fba0-4950-8aa3-5d1bb567081a | Primary Basic Entitlement Factor                                                                    | Approved      |
		| Template        | eca30f3a-c262-4435-84a8-13e3e1af67bf | Primary Pupil Count  SBS                                                                            | Approved      |
		| Template        | 60b82ba0-785f-4709-8905-020ec4765391 | Fringe Factor                                                                                       | Approved      |
		| Template        | 2e49d940-309f-448c-8e2d-50017ff31db8 | Basic Entitlement - Primary Funding                                                                 | Approved      |
		| Template        | baca161f-899c-4ab2-80eb-23d6b0bbb56c | Basic Entitlement - Primary SEN %                                                                   | Approved      |
		| Template        | 07a0046c-5e37-4521-86b7-dca803cd24f4 | London Fringe Factor                                                                                | Approved      |
		| Template        | f5e5e354-2529-49f3-baf0-d13e4e680097 | Basic Entitlement - Primary SEN Total                                                               | Approved      |
		| Template        | a82e0f02-aaf8-4528-be5b-980ef36c58d5 | KS3 Basic Entitlement Rate                                                                          | Approved      |
		| Template        | 94b9f8e4-bebe-4831-b9bc-43bc80886d5a | KS3 Basic Entitlement Factor                                                                        | Approved      |
		| Template        | 6a575190-07d7-41c7-9e8e-7f23c961e75c | KS3 Pupil Count  SBS                                                                                | Approved      |
		| Template        | 76fe73f1-59c5-400b-b406-1b3a79cea749 | Basic Entitlement - KS3 Funding                                                                     | Approved      |
		| Template        | 16308fe0-8b90-44b4-aa72-ae53914c5d6c | Basic Entitlement - KS3 SEN %                                                                       | Approved      |
		| Template        | 45049cd3-f7ff-4010-8614-5ba4940e4866 | Basic Entitlement - KS3 SEN Total                                                                   | Approved      |
		| Template        | ccf52cc7-11bf-4552-b7e2-9a4f01f7ecea | KS4 Basic Entitlement Rate                                                                          | Approved      |
		| Template        | d4832342-6fe3-4f7c-9d01-4958e35e6033 | KS4 Basic Entitlement Factor                                                                        | Approved      |
		| Template        | 3a99311e-7911-4495-bc3e-b918cf9a2f64 | KS4 Pupil Count  SBS                                                                                | Approved      |
		| Template        | a989532b-79fd-4ec2-a578-b212dcdd349d | Basic Entitlement - KS4 Funding                                                                     | Approved      |
		| Template        | c3bc3133-2db7-4b15-83f3-d4e6c29823b3 | Basic Entitlement - KS4 SEN %                                                                       | Approved      |
		| Template        | 9340b58d-39c9-4f82-bab6-b2dbaebc2037 | Basic Entitlement - KS4 SEN Total                                                                   | Approved      |
		| Template        | 84730c25-6d54-4d83-8a35-35635039a0b3 | Primary IDACI Band A Rate                                                                           | Approved      |
		| Template        | d27a0649-383a-42b0-9e9e-20b2e9b66013 | Primary IDACI Band A factor                                                                         | Approved      |
		| Template        | 249746cc-1a3f-48f0-98a6-1462be3c055f | Primary IDACI Band A funding                                                                        | Approved      |
		| Template        | 4e9e5f06-3e8a-45b5-9a5e-0e84fd6136b8 | Primary IDACI Band A SEN %                                                                          | Approved      |
		| Template        | 46de110c-a53a-40a6-ac3c-25c22933690d | Primary IDACI Band A SEN Total                                                                      | Approved      |
		| Template        | 27ec0c04-218d-430e-bb2a-4f779e5337c8 | Primary IDACI Band B Rate                                                                           | Approved      |
		| Template        | 439ea611-0fd4-4a8a-9795-3466a2d7d87d | Primary IDACI Band B Factor                                                                         | Approved      |
		| Template        | 36568277-f367-4c09-9bc5-d9d445fc0665 | Primary IDACI Band B funding                                                                        | Approved      |
		| Template        | c02e8a8a-a81b-4317-be9b-41cfe2621ba7 | Primary IDACI Band B SEN %                                                                          | Approved      |
		| Template        | 90aa01f2-aa21-4de7-95c5-8d5b65c0e621 | Primary IDACI Band B SEN Total                                                                      | Approved      |
		| Template        | 117103ed-e783-4f67-bd67-f1a15622355f | Primary IDACI Band C Rate                                                                           | Approved      |
		| Template        | 57ecf3c7-c4ab-4e55-b8a3-e6929722ae29 | Primary IDACI Band C Factor                                                                         | Approved      |
		| Template        | 8e8c8a46-89d2-4e31-a606-1b33d7e90c69 | Primary IDACI Band C funding                                                                        | Approved      |
		| Template        | 09917096-d399-484a-af15-82f5375fa6fa | Primary IDACI Band C SEN %                                                                          | Approved      |
		| Template        | 3e0a9c97-c89e-4c12-9fee-3a30b87dc2d5 | Primary IDACI Band C SEN Total                                                                      | Approved      |
		| Template        | 29608864-b47d-461b-9dc5-e6c064d6453c | Primary IDACI Band D SEN %                                                                          | Approved      |
		| Template        | 6e661f39-f7aa-44f3-b57e-4a4b2bfa772f | Primary IDACI Band D SEN Total                                                                      | Approved      |
		| Template        | 7c36427f-b696-4c9b-9646-ace4c7055aaf | Primary IDACI Band E Rate                                                                           | Approved      |
		| Template        | a7308e2c-9e81-44dc-940d-31dcbfea393d | Primary IDACI Band E Factor                                                                         | Approved      |
		| Template        | 1d88ae14-b911-48a9-a25a-78459c199ad9 | Primary IDACI Band E funding                                                                        | Approved      |
		| Template        | 46a69894-a912-4495-a7fd-cac29fe77e1d | Primary IDACI Band E SEN %                                                                          | Approved      |
		| Template        | 69bf74cd-8099-42b7-90a8-ebeecc5151f0 | Primary IDACI Band E SEN Total                                                                      | Approved      |
		| Template        | 04c5b6e5-b5c9-40c4-a04c-eae91b899c86 | Primary IDACI Band F Rate                                                                           | Approved      |
		| Template        | 058f94c5-4080-49fe-ab87-b92ada5bd86d | Primary IDACI Band F Factor                                                                         | Approved      |
		| Template        | f7c14f5f-fa54-4699-ac16-dbfc76bd0189 | Primary IDACI Band F funding                                                                        | Approved      |
		| Template        | bd2a3406-38f7-4f55-b951-a6a736728880 | Primary IDACI Band F SEN %                                                                          | Approved      |
		| Template        | 250b33cd-4f77-499b-99a8-1bdb1a39d7f8 | Primary IDACI Band F SEN Total                                                                      | Approved      |
		| Template        | dbffcced-ad33-4ad8-a366-b654261805c2 | Secondary IDACI Band A Rate                                                                         | Approved      |
		| Template        | 2ebf2483-71bd-482e-a797-84498d6a7545 | Secondary IDACI Band A factor                                                                       | Approved      |
		| Template        | 44e8b811-ab6a-4efd-9bd7-12c50974320f | Secondary Pupil Count SBS                                                                           | Approved      |
		| Template        | 37a799bc-c8b0-47be-9ef4-7ef33f2adb37 | Secondary IDACI Band A funding                                                                      | Approved      |
		| Template        | a8b1680d-111b-4978-ae66-79a2ff342e18 | Secondary IDACI Band A SEN %                                                                        | Approved      |
		| Template        | 376decb8-a3d2-4afd-913d-b68a40bc4ce5 | Secondary IDACI Band A SEN Total                                                                    | Approved      |
		| Template        | 3993030d-63fa-48fa-a977-a05a9699321b | Secondary IDACI Band B Rate                                                                         | Approved      |
		| Template        | 8b82c0f6-590b-4e4a-a218-74fec8c324a0 | Secondary IDACI Band B Factor                                                                       | Approved      |
		| Template        | 17773fc6-0500-42b5-9d99-7b6ea66731cb | Secondary IDACI Band B funding                                                                      | Approved      |
		| Template        | c384271b-66a8-4a7a-8b99-70fb862603ce | Secondary IDACI Band B SEN %                                                                        | Approved      |
		| Template        | 9c761e8d-724b-4320-b437-99ffc2de1e4f | Secondary IDACI Band B SEN Total                                                                    | Approved      |
		| Template        | 50c7a295-a5f3-4076-b484-0498f3a597d6 | Secondary IDACI Band C Rate                                                                         | Approved      |
		| Template        | 24c394d9-172a-43e1-b6b8-ffc8f9eba592 | Secondary IDACI Band C Factor                                                                       | Approved      |
		| Template        | fb808f92-8370-4b09-972c-0674472f810a | Secondary IDACI Band C funding                                                                      | Approved      |
		| Template        | f175ab50-f70d-4ef5-a469-d5847e0c5770 | Secondary IDACI Band C SEN %                                                                        | Approved      |
		| Template        | 212326eb-71ba-474a-93d0-b3f0ff843a74 | Secondary IDACI Band C SEN Total                                                                    | Approved      |
		| Template        | 7bc842e8-3525-4db9-8e3c-788d2a99d5db | Secondary IDACI Band D Rate                                                                         | Approved      |
		| Template        | cfe5a04e-fa4a-4124-8df2-a7465b5acbe7 | Secondary IDACI Band D Factor                                                                       | Approved      |
		| Template        | 57d3a9d0-318d-4500-b3be-a9eeaf4973e0 | Secondary IDACI Band D funding                                                                      | Approved      |
		| Template        | c064520f-f84f-40e2-8db0-09573581ca40 | Secondary IDACI Band D SEN %                                                                        | Approved      |
		| Template        | e4e582e9-fc5f-4511-ab29-4d423b30ab2d | Secondary IDACI Band D SEN Total                                                                    | Approved      |
		| Template        | 418d7e1f-f8fe-49f5-b578-03015a5ecb33 | Secondary IDACI Band E Rate                                                                         | Approved      |
		| Template        | 3a3c4c11-63c7-4ea2-8731-9aaf324604e9 | Secondary IDACI Band E Factor                                                                       | Approved      |
		| Template        | f506ecc9-edce-40f3-8a1b-42f03f36e2e7 | Secondary IDACI Band E funding                                                                      | Approved      |
		| Template        | e3aa6c5f-57d6-4901-8132-5157aad9fb1c | Secondary IDACI Band E SEN %                                                                        | Approved      |
		| Template        | e9302611-d9c2-429e-b383-a9300004d64a | Secondary IDACI Band E SEN Total                                                                    | Approved      |
		| Template        | 21dcf091-b09b-46c0-b9f5-1f421992f741 | Secondary IDACI Band F Rate                                                                         | Approved      |
		| Template        | 31264a49-e83b-4908-8114-11628128194b | Secondary IDACI Band F Factor                                                                       | Approved      |
		| Template        | 9f176522-f981-4021-937c-8d6b3dca5f27 | Secondary IDACI Band F funding`                                                                     | Approved      |
		| Template        | f82e601f-128d-4a37-bcdb-d1677853e08c | Secondary IDACI Band F SEN %                                                                        | Approved      |
		| Template        | cb58d29c-f81a-4d89-adba-ff29076bfccb | Secondary IDACI Band F SEN Total                                                                    | Approved      |
		| Template        | 377038dd-e124-4afd-8bdc-e45f39150ebd | Primary FSM Rate                                                                                    | Approved      |
		| Template        | 204e9392-eff0-4433-b81a-31968e3b0a25 | Primary FSM Factor                                                                                  | Approved      |
		| Template        | 3d6c4eb8-2a8a-4db8-86a2-182194f4a419 | Primary free school meals (FSM) Funding                                                             | Approved      |
		| Template        | e8d20a05-4a2e-4543-b44a-daef1899e6ea | Primary free school meals (FSM) SEN %                                                               | Approved      |
		| Template        | cd2c6719-b190-4903-b29d-a7c8ebe9390a | Primary free school meals (FSM) SEN Total                                                           | Approved      |
		| Template        | 35a97833-624b-4589-9327-38a42803996e | Primary FSM6 Rate                                                                                   | Approved      |
		| Template        | dc370e8b-fc76-44cb-9e08-c4f57b631dcb | Primary FSM6 Factor                                                                                 | Approved      |
		| Template        | bd794e50-e32f-4075-92cf-53fec4c3a044 | Primary FSM6 Funding                                                                                | Approved      |
		| Template        | 50ce2c4c-86b8-4fd5-8fc9-c68aad616f14 | Primary FSM6 SEN %                                                                                  | Approved      |
		| Template        | 358e784b-179b-4eb9-bd72-8d0218466bec | Primary FSM6 SEN Total                                                                              | Approved      |
		| Template        | 54081115-96df-4f67-90aa-da23e1820933 | Secondary FSM Rate                                                                                  | Approved      |
		| Template        | b3172c58-c2e3-41e1-ac2a-fd9ca2dde72a | Secondary FSM Factor                                                                                | Approved      |
		| Template        | 2d75f7ca-c68c-4eaa-9c06-bca4802f39fd | Secondary free school meals (FSM) Funding                                                           | Approved      |
		| Template        | 6ec89196-f0b8-4fa1-8453-3555aeb3ac5d | Secondary free school meals (FSM) SEN %                                                             | Approved      |
		| Template        | fd2cf131-5488-4503-97f5-7697e5078381 | Secondary free school meals (FSM) SEN Total                                                         | Approved      |
		| Template        | b8baa2c5-24ab-4499-8a3e-1590bcaa5234 | Secondary FSM6 Rate                                                                                 | Approved      |
		| Template        | 7598ef03-1d86-4981-8a89-f44820fbfdd5 | Secondary FSM6 Factor                                                                               | Approved      |
		| Template        | 05261e95-bb9c-4f7b-88a1-f313c92d5e50 | Secondary FSM6 Funding                                                                              | Approved      |
		| Template        | a47c4e52-90f3-4b8d-8635-3a997fdd9660 | Secondary FSM6 SEN %                                                                                | Approved      |
		| Template        | dd6781ef-dc12-483f-910f-7ff0b010e995 | Secondary FSM6 SEN Total                                                                            | Approved      |
		| Template        | 1e74f84b-333d-4d0d-be49-9388ca6aa4aa | Looked After Children Rate                                                                          | Approved      |
		| Template        | 7bd5ea1b-55c2-45fb-9748-2c1dbfa010b1 | Looked After Children Factor                                                                        | Approved      |
		| Template        | 636c17a8-6b21-4939-8098-e804f2988e75 | Total Pupil Count SBS                                                                               | Approved      |
		| Template        | c4c2c50e-f669-46e1-8b55-f6075712c90f | Looked After Children Funding                                                                       | Approved      |
		| Template        | 41eca670-8db0-435d-8b9a-91567571eb3d | Looked After Children SEN %                                                                         | Approved      |
		| Template        | 2daed5a4-1720-4214-973a-3ab961ca9abc | Looked After Children SEN Total                                                                     | Approved      |
		| Template        | 5dbcd2e2-cb73-4941-9e5f-078aeec3feea | Primary Prior attainment rate                                                                       | Approved      |
		| Template        | 680881f6-b1c1-492b-8b70-561d28097267 | Primary Prior attainment factor                                                                     | Approved      |
		| Template        | a76f7851-062b-40d0-8db8-ce9e64b79538 | Primary Attainment - Low Primary Prior Attainment Funding                                           | Approved      |
		| Template        | 1e201223-3cb2-4351-8ec3-6ba9dd4850a8 | Primary Attainment - Low Primary Prior Attainment SEN %                                             | Approved      |
		| Template        | f2054f45-cf93-4175-9dd1-c7f15dda5407 | Primary Attainment - Low Primary Prior Attainment SEN Total                                         | Approved      |
		| Template        | b6215036-7af8-427d-8e47-231b9f5bc9e0 | Secondary Prior Attainment Rate                                                                     | Approved      |
		| Template        | 58183918-c2c4-4af8-9dd7-4afc66f589af | Secondary Prior Attainment Factor                                                                   | Approved      |
		| Template        | 477c37d4-fe93-4fb2-9244-613a0c3aebec | Secondary Attainment - Secondary Pupils not achieving the expected standards in KS2 Tests Funding   | Approved      |
		| Template        | 6d4e439b-fd0d-4968-a77e-b76db92584ae | Secondary Attainment - Secondary Pupils Not Achieving the expected standards in KS2 Tests SEN %     | Approved      |
		| Template        | a7838e21-fae1-475b-850b-83bfe37e0fb5 | Secondary Attainment - Secondary Pupils Not Achieving the expected standards in KS2 Tests SEN Total | Approved      |
		| Template        | fcdb6c77-434c-4b49-833b-a2629208adb7 | Primary EAL1 Rate                                                                                   | Approved      |
		| Template        | 2166eacc-edc9-4bed-a326-c0f4d32117e3 | Primary EAL1 Factor                                                                                 | Approved      |
		| Template        | fa51564f-914d-4d5a-9024-bf053a9c17c8 | Primary EAL Band 1 Funding                                                                          | Approved      |
		| Template        | f13433a0-eea7-41e6-9986-2dcb2bd328a3 | Primary EAL Band 1 SEN %                                                                            | Approved      |
		| Template        | 9fabe999-c4cc-45d2-b8d7-d2a5cf9f2c09 | Primary EAL Band 1 SEN Total                                                                        | Approved      |
		| Template        | 76fe4544-c380-4e7b-8544-6684f8f133b3 | Primary EAL2 Rate                                                                                   | Approved      |
		| Template        | b903444e-c0bf-4006-bd00-6d7865b5c1dd | Primary EAL2 Factor                                                                                 | Approved      |
		| Template        | e3fc8899-a630-431f-a36c-787fd9384826 | Primary EAL Band 2 Funding                                                                          | Approved      |
		| Template        | 4094630d-319e-4695-8d2c-d73fa4e696b4 | Primary EAL Band 2 SEN %                                                                            | Approved      |
		| Template        | 2c0b7bc0-b2b7-4d3e-9685-6deaae56d9f2 | Primary EAL Band 2 SEN Total                                                                        | Approved      |
		| Template        | e420c1d4-ff26-424f-8917-30ea72fdcd73 | Primary EAL3 Rate                                                                                   | Approved      |
		| Template        | e1178be4-5924-44f7-a8a8-8bb44819c570 | Primary EAL3 Factor                                                                                 | Approved      |
		| Template        | 058f22ea-ccc7-4f2d-9e2b-fffc6d7fba4b | Primary EAL Band 3 Funding                                                                          | Approved      |
		| Template        | a19df2b3-63d8-47d2-9405-c8863c7a34f7 | Primary EAL Band 3 SEN %                                                                            | Approved      |
		| Template        | 982693f4-c015-463f-ad0c-8c5e9c6ffab8 | Primary EAL Band 3 SEN Total                                                                        | Approved      |
		| Template        | f0ea2e21-1ade-4cb7-84ac-539952c6199c | Secondary EAL1 Factor                                                                               | Approved      |
		| Template        | c8898af8-188a-4154-809c-e50f6e230025 | Secondary EAL1 Rate                                                                                 | Approved      |
		| Template        | 78183215-4c23-4b4f-a9bd-9e5f8caf61ef | Secondary EAL Band 1 Funding                                                                        | Approved      |
		| Template        | 77e37a97-32c6-4326-876e-872143251629 | Secondary EAL Band 1 SEN %                                                                          | Approved      |
		| Template        | 1c3df7fb-a773-4f8d-83bb-2e4ab2ae50c4 | Secondary EAL Band 1 SEN Total                                                                      | Approved      |
		| Template        | cd841069-71f5-485b-a9fe-0372d3883306 | Secondary EAL2 Rate                                                                                 | Approved      |
		| Template        | 00e22126-05c9-4c3e-a6ff-1187e67f5090 | Secondary EAL2 Factor                                                                               | Approved      |
		| Template        | e6181805-68ac-480d-89ab-1b9642f6a9be | Secondary EAL Band 2 Funding                                                                        | Approved      |
		| Template        | 52000633-dc4f-47b1-8fed-af823e2c0ea3 | Secondary EAL Band 2 SEN %                                                                          | Approved      |
		| Template        | d01a0579-a898-4c11-9822-34c21aff3c42 | Secondary EAL Band 2 SEN Total                                                                      | Approved      |
		| Template        | fb5770ce-4079-4416-bb08-b9f5ff4ed39f | Secondary EAL3 Rate                                                                                 | Approved      |
		| Template        | a90f3e18-5674-4e11-8301-200a437d2de1 | Secondary EAL3 Factor                                                                               | Approved      |
		| Template        | af7b5468-275f-472b-a3d8-57bc9deea236 | Secondary EAL Band 3 Funding                                                                        | Approved      |
		| Template        | 40b62036-9bef-4c12-a5e9-ec0606dc1e23 | Secondary EAL Band 3 SEN %                                                                          | Approved      |
		| Template        | bd86816e-63fd-447c-9975-12b2e1013921 | Secondary EAL Band 3 SEN Total                                                                      | Approved      |
		| Template        | 89416254-2575-434b-bd58-ef9296ddff66 | Primary Mobility Rate                                                                               | Approved      |
		| Template        | 33c00845-bbe1-4d16-b191-d9cf7814622b | Primary Mobility Factor                                                                             | Approved      |
		| Template        | c75abe6b-3b8d-4872-ba87-48468bcba427 | Primary Mobility Funding                                                                            | Approved      |
		| Template        | 3417c52b-5fff-4e15-8fce-ba62684c787a | Primary Mobility SEN %                                                                              | Approved      |
		| Template        | 0aebb694-f29e-47f7-88d9-5c794a3695dc | Primary Mobility SEN Total                                                                          | Approved      |
		| Template        | 1f76dae7-d72a-42a0-9ec4-9bb106ae0156 | Secondary Mobility Rate                                                                             | Approved      |
		| Template        | 8138e75b-777e-44e8-9609-d3fde44f85b3 | Secondary Mobility Factor                                                                           | Approved      |
		| Template        | 67b09d13-ef01-4f8b-82bf-3c83bf0367b9 | Secondary Mobility Funding                                                                          | Approved      |
		| Template        | 6454f2d7-49a6-48f6-b4e6-745240bf2ea7 | Secondary Mobility SEN %                                                                            | Approved      |
		| Template        | 6ca9923c-86ef-4165-8c77-a0998f0a8b48 | Secondary Mobility SEN Total                                                                        | Approved      |
		| Template        | f2a302e2-5cf5-45f6-9944-0809da9e4f78 | Primary Lump Sum Value                                                                              | Approved      |
		| Template        | 111200c6-07c8-4424-967b-3b2c3d1d1b0f | Primary Lump Sum Factor                                                                             | Approved      |
		| Template        | 0eeeab9e-8a5e-40bd-9439-1e22ed6fa211 | Primary Lump Sum Funding                                                                            | Approved      |
		| Template        | e61cf665-47c0-4149-95fb-0f09de81f97d | Primary Lump Sum SEN %                                                                              | Approved      |
		| Template        | 7e238559-0d24-434c-8edb-c964e5e5ef3b | Primary Lump Sum SEN Total                                                                          | Approved      |
		| Template        | 87067f53-7eac-4e86-883f-3db34e5276cf | Secondary Lump Sum Factor                                                                           | Approved      |
		| Template        | 3ba29f60-eb82-433b-bb97-8f10d47e5cc9 | Secondary Lump Sum Value                                                                            | Approved      |
		| Template        | ac63be86-3b6f-4c77-9b15-e45fe925c9bf | Secondary Lump Sum Funding                                                                          | Approved      |
		| Template        | eb9d407e-24b6-4c4c-9b3a-1e0cf05c6e0c | Secondary Lump Sum SEN %                                                                            | Approved      |
		| Template        | 4c9cf10d-6d7c-4e3b-886d-d772b89e2814 | Secondary Lump Sum SEN Total                                                                        | Approved      |
		| Template        | 62eb6b69-1521-44d5-b58c-c2b3dcfc1e02 | Sparsity Distance                                                                                   | Approved      |
		| Template        | b5bfef59-1fd8-4475-b815-a7fae50a882c | Sparsity Distance Threshold                                                                         | Approved      |
		| Template        | 5c88baa2-f663-4982-bd11-488d276b54a9 | No of Primary Year Groups                                                                           | Approved      |
		| Template        | 162d56e6-8b92-46ac-a638-b387d4d79ce1 | No of Secondary Year Groups                                                                         | Approved      |
		| Template        | 9a6b6afb-751e-4291-a29f-0081122c647b | Average year group size                                                                             | Approved      |
		| Template        | fdfacd09-bebe-4fde-a2d0-a8685fc0ac37 | Sparsity Distance Evaluation                                                                        | Approved      |
		| Template        | a8973266-8d72-45cd-9cb8-e14f0870b57c | Average year group size evaluation                                                                  | Approved      |
		| Template        | cb8bb595-7cd1-4135-ba02-ed3d1395df27 | Sparsity Criteria Evaluation                                                                        | Approved      |
		| Template        | 2afe77ee-8168-4a07-95f1-f7b557206b63 | Sparsity Average Year Group Size Threshold                                                          | Approved      |
		| Template        | 40fbc5d3-6eb9-47f7-b834-91ba43169d18 | NFF Weighting evaluation                                                                            | Approved      |
		| Template        | d0d677a4-5f82-4714-81a6-d5f63aa8aa8e | Sparsity NFF Taper                                                                                  | Approved      |
		| Template        | 3c000f9b-4995-4903-81b0-e4a587f54dd8 | Sparsity Methodology                                                                                | Approved      |
		| Template        | 3e9d6f38-5098-4793-8268-bcd2c6753830 | Lump Sum Weighting                                                                                  | Approved      |
		| Template        | feebb937-77b4-4392-b1c6-f4bef176000c | Sparsity Taper Weighting                                                                            | Approved      |
		| Template        | 8a73996f-6983-4785-9dd2-3c3706c928b9 | Sparsity NFF Weighting                                                                              | Approved      |
		| Template        | 2f6a0828-2d2c-4bbf-8bc9-150256f594f1 | Sparsity Methodology Weighting Selection                                                            | Approved      |
		| Template        | 6bc558ab-b158-44b8-b7f8-7a6befbf89bb | Sparsity Evaluation                                                                                 | Approved      |
		| Template        | 5317c77e-07a1-4a62-9109-12ab581101a5 | Sparsity Lump Sum                                                                                   | Approved      |
		| Template        | ff122d10-4963-4b27-b7b6-99c046d898b4 | Sparsity Weighting                                                                                  | Approved      |
		| Template        | a26fe811-91e0-4b9d-9fe9-613d445ceb77 | Sparsity Funding                                                                                    | Approved      |
		| Template        | 83945ff2-76f7-4b4a-addc-cd2a13069063 | Sparsity SEN %                                                                                      | Approved      |
		| Template        | 09408dd6-45a1-4ce0-bbb6-119a85e3aff3 | Sparsity SEN Total                                                                                  | Approved      |
		| Template        | 186490d0-8f6c-4c0b-ad41-8b06ca2adc74 | Split site weighting                                                                                | Approved      |
		| Template        | 2a0f0f2d-b945-4e80-97cf-37cba8fa32a5 | Split Site total                                                                                    | Approved      |
		| Template        | 5f94bc40-debf-49ce-8cb8-f399de31a9fb | Split Site Full year SEN %                                                                          | Approved      |
		| Template        | 0f4f02c5-9d56-48f0-9c26-0d89418e5641 | Split Site Full year SEN Total                                                                      | Approved      |
		| Template        | 433f02eb-4d12-497b-9fb6-67f23daab6b4 | Front Loaded PFI Indicator                                                                          | Approved      |
		| Template        | c496d274-15cb-450e-93aa-ccc1c1d210b1 | PFI APT Data                                                                                        | Approved      |
		| Template        | 41fd7633-babd-437d-89a8-7a6756a77463 | PFI Type value selection                                                                            | Approved      |
		| Template        | d4b7bedc-98bd-4b9a-9dbc-b6b61ee00625 | PFI weighting                                                                                       | Approved      |
		| Template        | 56ed9478-ad68-4d4d-afcd-ce96f633d963 | Standard PFI                                                                                        | Approved      |
		| Template        | 9016b5de-cd6e-4025-9bae-92cdd16560fd | Standard PFI SEN %                                                                                  | Approved      |
		| Template        | d5d1d35c-459a-4036-9589-ad6a4bc96348 | Standard PFI SEN Total                                                                              | Approved      |
		| Template        | 3f8a09a0-4fc2-4346-9f34-b26b0d9b8bff | Primary IDACI Band D Rate                                                                           | Approved      |
		| Template        | d4a02087-e3a9-4498-8dc3-b1662a587b60 | Primary IDACI Band D Factor                                                                         | Approved      |
		| Template        | 11fbe321-1b2b-4221-bc18-67ebc07225d3 | Primary IDACI Band D funding                                                                        | Approved      |
		| Template        | d6be331a-253f-4363-a375-bafca45f4879 | Exceptional Circumstance 1 Funding                                                                  | Approved      |
		| Template        | 58d9d140-5a7b-4039-8877-f0d2aa71159a | London Fringe Eligible Funding                                                                      | Approved      |
		| Template        | f41ab80c-e39b-43bc-aabd-6aeb70fc38fa | London Fringe Funding                                                                               | Approved      |
		| Template        | 0e5c8575-434c-44b3-b62a-52b3cf90cde2 | Prior Year Adjustment to SBS                                                                        | Approved      |
		| Template        | 3378a1bb-8a56-4ab1-a608-f118dd32c036 | APT NEWISB Rates                                                                                    | Approved      |
		| Template        | 63e772cc-01c4-404e-88c1-e0b0e676ab28 | APT Approved Additional Premises costs to exclude                                                   | Approved      |
		| Template        | 3a2af9de-e573-4e24-b0cd-577ddc231c22 | SBS Base Allocation                                                                                 | Approved      |
		| Template        | cf92aa4f-03f3-4cd3-b086-a12b43039226 | MFL Rate                                                                                            | Approved      |
		| Template        | 38625ca7-076e-4da0-bee9-745306951fd8 | SBS Per Pupil Allocation                                                                            | Approved      |
		| Template        | bb412013-aa9b-4568-8309-d37420932eab | MFL Evaluation                                                                                      | Approved      |
		| Template        | 1e51c13a-b2a7-462a-b31c-649034f7819e | MFL Uplift per pupil                                                                                | Approved      |
		| Template        | 141bfd4a-31a9-41b0-a2d2-bc6237abd590 | MFL Adjustment SEN %                                                                                | Approved      |
		| Template        | 52262e18-d970-4a63-a022-d6a2cd5b99d0 | MFL Adjustment                                                                                      | Approved      |
		| Template        | 9a06c82b-80ae-4620-b8bf-1d976b98cfc4 | Exceptional Circumstances 1 SEN %                                                                   | Approved      |
		| Template        | 08fa534f-d2bb-409d-9d0b-46ee9891500d | Exceptional Circumstances 1 SEN Total                                                               | Approved      |
		| Template        | d3acda35-dde9-4fbc-807e-e24ed3bdb28b | Exceptional Circumstance 2 Funding                                                                  | Approved      |
		| Template        | 2826e609-f187-4912-9a89-23c8c93f98e8 | Exceptional Circumstances 2 SEN %                                                                   | Approved      |
		| Template        | 4ce5468d-bda6-4d5a-bae5-d958d4c99508 | Exceptional Circumstances 2 SEN Total                                                               | Approved      |
		| Template        | f0951107-9c48-4f37-897c-981dada3efc9 | Exceptional Circumstances 3 Funding                                                                 | Approved      |
		| Template        | 7a4dc0d7-5aa5-4ec4-8e9a-72f5209c5ac7 | Exceptional Circumstances 3 SEN %                                                                   | Approved      |
		| Template        | 670cd089-f292-4539-b900-64534f5eccf3 | Exceptional Circumstances 3 SEN Total                                                               | Approved      |
		| Template        | ef7b1a48-815d-49b6-9b36-ec604fbfb72b | Exceptional Circumstance 4 Funding                                                                  | Approved      |
		| Template        | c2e0d2f8-2277-4445-a1f9-eca963b78a4c | Exceptional Circumstances 4 SEN %                                                                   | Approved      |
		| Template        | 5ad44b8c-97d9-4f8f-927a-d242240b9df7 | Exceptional Circumstances 4 SEN Total                                                               | Approved      |
		| Template        | c6d9191a-9268-4246-86d8-596ca99124e8 | Exceptional Circumstance 5 Funding                                                                  | Approved      |
		| Template        | 3e4ff4f7-b78c-4821-b559-40b3ee581e37 | Exceptional Circumstances 5 SEN %                                                                   | Approved      |
		| Template        | 919af526-2140-4ce8-a03e-9e478bd7a52e | Exceptional Circumstances 5 SEN Total                                                               | Approved      |
		| Template        | ad74e7af-16a3-47f9-9bbc-657f3062293f | Exceptional Circumstances 6 Funding                                                                 | Approved      |
		| Template        | 596f79fa-f67e-4b47-a945-7bd65b223630 | Exceptional Circumstances 6 SEN %                                                                   | Approved      |
		| Template        | 2fad3459-ba4a-4eaa-a6e7-d8ba9d38261b | Exceptional Circumstances 6 SEN Total                                                               | Approved      |
		| Template        | 3eb7be55-f247-4803-962f-b2c0676581b6 | Exceptional Circumstance 7 Funding                                                                  | Approved      |
		| Template        | 88cbb66c-a32c-47bc-bc4b-9894b72e6691 | Exceptional Circumstances 7 SEN %                                                                   | Approved      |
		| Template        | 1a1ba3be-f479-454c-a8d2-a0fe9c99e192 | Exceptional Circumstances 7 SEN Total                                                               | Approved      |
		| Template        | 31c653cf-af45-4990-9c91-ace572bbba12 | Previous Full Year SBS Total                                                                        | Approved      |
		| Template        | 14718456-a287-4722-a56f-60be7916e415 | Current Year Lump Sum Including Fringe                                                              | Approved      |
		| Template        | 683b3d1e-c656-444f-ae45-837c9756b19e | Current Year Sparsity Total                                                                         | Approved      |
		| Template        | 2cfdb755-89c4-49d1-8854-a1aa439e4b44 | Previous Year Approved MFG Exclusions Total                                                         | Approved      |
		| Template        | c855e356-54ee-4f2d-a866-95a99c3db76a | Total Technical Adjustment to Baseline Year Funding                                                 | Approved      |
		| Template        | 0f3836da-1b14-49e9-aed9-9a058c04f9c6 | Baseline Current Year Lump Sum Including Fringe                                                     | Approved      |
		| Template        | 12ae9f9a-d9fd-4852-8ed9-114503ba9e6b | Adjusted Previous Year School Budget Share                                                          | Approved      |
		| Template        | f6281310-00aa-42f0-a4a4-39541c3c4caa | Previous Year Total Pupil Count MFG                                                                 | Approved      |
		| Template        | 55c78b2d-1340-45a5-846f-8d614dc55c92 | Previous Year MFG Unit Value                                                                        | Approved      |
		| Template        | e1a30933-0fad-49ec-beb3-34038d2fb5b2 | MFG Floor                                                                                           | Approved      |
		| Template        | 9a3f57ef-d715-4ce2-bd94-45e03d11cbfa | MFG Floor Constant                                                                                  | Approved      |
		| Template        | a6c8f2f9-1d10-487c-a6bc-d6fda82e3f5c | Current year MFG Primary pupil count                                                                | Approved      |
		| Template        | 9fcda229-77a0-4578-ae98-af5608d9c654 | Current year MFG secondary pupil count                                                              | Approved      |
		| Template        | a90800cd-bdcf-425b-8916-1b8649abc8ac | Minimum Value Per pupil                                                                             | Approved      |
		| Template        | c9c068a7-142a-457c-8e92-bddd8cc6f8e4 | Current Year Total MFG Pupil Count                                                                  | Approved      |
		| Template        | 0dd75339-0803-46e5-af5d-2b012d19c4ce | Current Year SBS Total                                                                              | Approved      |
		| Template        | 8079034a-6c5d-44f0-bd3e-6ead8e94bb56 | Current Year Approved MFG Exclusions Total                                                          | Approved      |
		| Template        | e864becd-892d-4656-912a-48f4802bce76 | Total Current Year Technical Adjustments                                                            | Approved      |
		| Template        | a7fe509d-b9e6-4dc8-a481-7291fb7eab95 | Current year Guaranteed level of funding                                                            | Approved      |
		| Template        | 9c09a321-eca4-497b-bb41-9667a8d6bcfa | Total Current Year MFG Budget                                                                       | Approved      |
		| Template        | 72e24f7e-5dcf-4c2e-b88c-4277d8efbad4 | MFG Adjustment evaluation                                                                           | Approved      |
		| Template        | fe3bcb3b-3998-40bc-91fe-802110ab59d3 | Extent to which percentage change falls below MFG floor                                             | Approved      |
		| Template        | 38fedaa4-4f2b-435e-a457-0044513ae4d2 | Current Year MFG unit Value                                                                         | Approved      |
		| Template        | 0b4a43d4-88f7-47fa-89d5-7e17113739e0 | Change in MFG Unit Value                                                                            | Approved      |
		| Template        | cfbb4880-21ee-4265-8918-b61ca48b8af2 | Growing School Check                                                                                | Approved      |
		| Template        | d7b2b451-4e05-4bd6-9fe2-2fe9d082f1d5 | Percentage change in MFG unit value                                                                 | Approved      |
		| Template        | 698822c6-ba43-424f-ad6d-726ff81aa95b | LA Capping Factor                                                                                   | Approved      |
		| Template        | bff2812a-a128-405c-ad87-5a917b986853 | Growing School Evaluation                                                                           | Approved      |
		| Template        | 174e95d0-11cd-4d56-8d5e-8edcf0faa9f8 | Does the LA apply capping and scaling                                                               | Approved      |
		| Template        | 0e137c2c-d55e-4f76-ab21-eab66d7b0ba6 | Cap percentage change evaluation                                                                    | Approved      |
		| Template        | 31e97413-c3a3-4f91-846d-1481e2ea71f9 | Extent to which percentage change exceed the cap                                                    | Approved      |
		| Template        | 1df9c246-57ad-4987-a598-a7c16e528b14 | LA Scaling Factor                                                                                   | Approved      |
		| Template        | 5e4d20e9-ff72-4796-86a2-22f17444dce0 | Scaled Factor Applied to Excess Above Cap                                                           | Approved      |
		| Template        | b1c698f3-dd53-478b-a488-92fba43236e6 | Affordability Adjustment Value                                                                      | Approved      |
		| Template        | 59d5f9df-8082-42ca-8385-0d602322c926 | MFL Funding                                                                                         | Approved      |
		| Template        | 7765d8a9-5c06-42d7-86ae-d43002a73d4c | APT Premises Exceptions                                                                             | Approved      |
		| Template        | 2057be6e-ea03-4262-8c93-076725659d86 | PFI                                                                                                 | Approved      |
		| Template        | 7df29588-9a41-4375-b205-530604ada14b | MFL Check Value Constant                                                                            | Approved      |
		| Template        | 4fba8423-b5fe-41e7-96d5-125b23709286 | Minimum Funding                                                                                     | Approved      |
		| Template        | 7ea4ea56-7df3-42bd-9561-0f1ca2240328 | MFL To check if I can clone                                                                         | Approved      |
		| Template        | 274a745b-cbf6-4852-a09b-c7c606d92298 | MFL Check Value Evaluation                                                                          | Approved      |
		| Template        | ca588a55-e282-4ebc-a23f-e4aaff6f17a5 | Actual MFL Eligible Funding                                                                         | Approved      |
		| Template        | 4aef4fa3-eff9-4216-9cb6-d553e163f8fb | Affordability Adjustment evaluation                                                                 | Approved      |
		| Template        | 3e636fc2-10c6-4996-a143-bda19fef4fa8 | MFL Check Value                                                                                     | Approved      |
		| Template        | 4ca8af79-2dc4-4a76-94c1-4dd05cde9c81 | MFG Adjustment Value                                                                                | Approved      |
		| Template        | d65aae08-2b13-45a0-985d-0de04b5b5386 | Minimum Funding Level Adjustment post cap and scaling                                               | Approved      |
		| Template        | 7af1f923-4336-466f-bf56-773403c82598 | MFG Overall Net Cash Adjustment                                                                     | Approved      |
		| Template        | 7e282e12-6f81-408e-be6d-ff693f498635 | MFG Overall Net Cash SEN %                                                                          | Approved      |
		| Template        | 679d4d47-1eaf-4dc0-be20-0370600fefec | Rate Unoccupied Place Rate                                                                          | Approved      |
		| Template        | abd5d8cd-2382-4347-9348-062dd27d8906 | Special Unoccupied Pre 16 Places                                                                    | Approved      |
		| Template        | 7832f65c-5be3-4905-9592-029df8f258b4 | Proportion of year open                                                                             | Approved      |
		| Template        | d07eede9-9a1d-46ac-83d2-91671737d41a | Days open in year                                                                                   | Approved      |
		| Template        | 1110f308-d7f1-4364-b4bd-d0ea050c9864 | Special Unoccupied Total                                                                            | Approved      |
		| Template        | eefd8b04-14a6-49b5-9943-5a10864a7929 | Special unoccupied - Full year subtotal                                                             | Approved      |
		| Template        | bd40f3cb-7b04-4aa7-a146-40f40f3a2f64 | Rate Occupied Place Rate                                                                            | Approved      |
		| Template        | 5151e6c2-6274-4283-ba7d-44c9357612a3 | Special Occupied Pre 16 Places                                                                      | Approved      |
		| Template        | 91be94f0-a918-4ac5-9a7e-3426045202b3 | Special Occupied Total                                                                              | Approved      |
		| Template        | 287d66e5-350c-445f-bd3b-81153de5501d | Special Occupied - Full year subtotal                                                               | Approved      |
		| Template        | be6a08ac-2103-4fcd-b926-385873fdabe1 | Alternative Provision Rate                                                                          | Approved      |
		| Template        | e9d7c408-31a0-4bd4-a33a-9e35b172bf3a | Alternative Provision Pre 16 Places                                                                 | Approved      |
		| Template        | 42020112-29ac-4f09-a93f-897c5da99b2d | Alternative Provision Total                                                                         | Approved      |
		| Template        | 7cf0d5c7-f00b-4943-81d4-3181a449c7fc | Alternative Provision - Full year subtotal                                                          | Approved      |
		| Template        | afe4b7fd-1e0c-4e53-95f5-a4389ddbac89 | Hospital Places                                                                                     | Approved      |
		| Template        | 1238c139-e22f-434b-975a-d34697557ab4 | Hospital Provision Funding                                                                          | Approved      |
		| Template        | c1e2f5b5-b7b2-4ef3-8388-0b37ac03eb88 | Hospital Provision - Full year subtotal                                                             | Approved      |
		| Template        | cbc46773-412d-4439-a590-56d0e99bcafa | Pre-16 High Needs - Full year subtotal                                                              | Approved      |
		| Template        | 67a08e21-95fb-4b94-b2f4-d523228e4849 | Post Opening Grant - Per Pupil Resources                                                            | Approved      |
		| Template        | 1423eae9-cec9-4749-a23b-c15ac7f82e8a | Post Opening Grant - Leadership Diseconomies                                                        | Approved      |
		| Template        | d00a00ce-0a84-431a-9652-965b7dc42a04 | Start up Grant Part A                                                                               | Approved      |
		| Template        | be54fbce-c20e-41aa-9d5a-f8781cb40718 | Start up Grant Part B                                                                               | Approved      |
		| Template        | d4a5fc65-ead4-4dc2-b04e-a958eed65d5d | Basic Entitlement - Primary Full Year subtotal                                                      | Approved      |
		| Template        | dd9751cc-af3f-40b6-83d5-c22e2c7f291a | Basic Entitlement KS3 full year subtotal                                                            | Approved      |
		| Template        | 535ce184-f3c7-403d-88f4-a59368e4e02c | Basic Entitlement - KS4 Full Year Subtotal                                                          | Approved      |
		| Template        | 2d67068b-0d4b-41c1-be26-2a8ebde210cb | Primary IDACI Band A Full Year Subtotal                                                             | Approved      |
		| Template        | e343b36a-4f08-484d-8167-ecc612bd2c56 | Primary IDACI Band B - Full Year Subtotal                                                           | Approved      |
		| Template        | abdf18bb-7302-4adb-ab01-1789435f1f63 | Primary IDACI Band C - Full Year Subtotal                                                           | Approved      |
		| Template        | 266cbd73-06e7-481c-80c1-1491418b5906 | Primary IDACI Band D - Full Year Subtotal                                                           | Approved      |
		| Template        | d14b5ea4-3d0f-4fe1-87d2-5d00954388e9 | Primary IDACI Band E - Full year subtotal                                                           | Approved      |
		| Template        | 39878aab-f03c-468d-973d-dcc2ae45a23b | Primary IDACI Band F - Full year subtotal                                                           | Approved      |
		| Template        | 51102ce4-dca9-43dc-8ee1-d5d467ea2752 | Secondary IDACI Band A - Full year subtotal                                                         | Approved      |
		| Template        | 2a9b0f24-2001-4074-b987-dd32fcf610f5 | Secondary IDACI Band B - Full year subtotal                                                         | Approved      |
		| Template        | 020bf6b3-b537-4a59-a432-9ed366cf43b5 | Secondary IDACI Band C - Full year subtotal                                                         | Approved      |
		| Template        | 4260a95a-7e89-4646-80ed-7611b9596bf3 | Secondary IDACI Band D - Full Year Subtotal                                                         | Approved      |
		| Template        | 77d7c952-5c8f-4951-a1ac-1018263b27bc | Secondary IDACI Band E - Full year subtotal                                                         | Approved      |
		| Template        | 9dc41696-4c4d-47bd-b8e4-11a012b311fc | Secondary IDACI Band F - Full year subtotal                                                         | Approved      |
		| Template        | b95f2a8c-2f19-4cfc-adfc-124f828dc4b2 | Primary free school meals - Full year subtotal                                                      | Approved      |
		| Template        | 24b29c3d-a165-4c39-a33b-9b5860003ec5 | Primary FSM6 - Full year subtotal                                                                   | Approved      |
		| Template        | f0afc9a0-f6bc-4a19-ae1b-6ad8e8cb0d1b | Secondary Free School meals - Full year subtotal                                                    | Approved      |
		| Template        | b901b173-1cea-471a-9e5d-feb8aed7bddb | Secondary FSM6 - Full year subtotal                                                                 | Approved      |
		| Template        | 9c096a86-f468-4787-bf6f-edcd85b78a8c | Primary prior attainment - Full year subtotal                                                       | Approved      |
		| Template        | 941f848d-0c98-4365-8676-4dbc376b02af | Secondary prior attainment - Full year subtotal                                                     | Approved      |
		| Template        | 4f833685-26ed-4c23-b025-95158146ed39 | Primary EAL Band 1 - Full Year Subtotal                                                             | Approved      |
		| Template        | b5889a12-97bd-4a74-aa49-9d2d4471b3bd | Primary EAL Band 2 - Full year subtotal                                                             | Approved      |
		| Template        | 4b2999f9-e16b-4150-88b0-29ea6ce08afc | Primary EAL Band 3 - Full year subtotal                                                             | Approved      |
		| Template        | ae768083-e202-48af-93ba-c2ae41f6064a | Secondary EAL Band 1 - Full year subtotal                                                           | Approved      |
		| Template        | 1882032c-da6c-4d3b-b0f2-7789ea527954 | Secondary EAL Band 2 - Full year subtotal                                                           | Approved      |
		| Template        | ffcc3dee-7255-463e-8e14-7bae34283d4f | Secondary EAL Band 3 - Full year subtotal                                                           | Approved      |
		| Template        | b04ae7a0-4783-499b-af58-a7dcf3926e0a | Primary Mobility - Full year subtotal                                                               | Approved      |
		| Template        | 12e0caf0-89e6-4e14-bf7e-ee5900233611 | Secondary Mobility - Full year subtotal                                                             | Approved      |
		| Template        | 8219ab37-2ea3-44bf-acf7-c2e3500ccb20 | Basic Entitlement - Full Year Subtotal                                                              | Approved      |
		| Template        | 37df406f-af2d-4c14-b5e9-c737f752e8b0 | Deprivation - Full Year Subtotal                                                                    | Approved      |
		| Template        | 2174ef51-f916-4354-919a-2e05465fd66c | Looked After Children - Full year subtotal                                                          | Approved      |
		| Template        | 0706444a-261b-4c38-aa24-93c1257d5b01 | Prior attainment - Full year subbtotal                                                              | Approved      |
		| Template        | 8a8c6c57-96ff-47c4-b97f-219c76989251 | EAL Funding - Full year subtotal                                                                    | Approved      |
		| Template        | aff5b63f-53c0-45d2-a054-29275219adfe | Mobility Funding - Full year subtotal                                                               | Approved      |
		| Template        | 50ac16e0-2820-430d-817a-9ea39552d1b6 | Primary Lump Sum - Full year subtotal                                                               | Approved      |
		| Template        | fa52c84c-84b4-4027-a4ec-365810e9dc0f | Secondary Lump Sum - Full year subtotal                                                             | Approved      |
		| Template        | 41eddf91-3af1-4ccc-aac3-6e8810785ee4 | Sparsity Funding - Full year subtotal                                                               | Approved      |
		| Template        | 630f6425-56d3-45f6-8b74-3c51e154f9a6 | Lump sum - Full Year subtotal                                                                       | Approved      |
		| Template        | 21881ce6-13c0-4112-aa06-a65ed54288e6 | Split Site - Full Year subtotal                                                                     | Approved      |
		| Template        | 3ec8ff2e-fd60-40ca-84dc-9d45821bd845 | PFI - Full year subtotal                                                                            | Approved      |
		| Template        | ad49eea6-3370-448a-8730-1f8884338956 | London Fringe - Full year subtotal                                                                  | Approved      |
		| Template        | ed5cff68-0b99-49f1-8205-0826bede11f6 | MFL - Full year subtotal                                                                            | Approved      |
		| Template        | d15afb54-3321-4820-9410-c1adf37f6ca6 | Front Loaded PFI                                                                                    | Approved      |
		| Template        | 82588b02-5fa5-4b50-8455-e91bbdea72ae | PFI Funding - Full year subtotal                                                                    | Approved      |
		| Template        | 9c3d1b02-c585-4b43-a09c-90d6339b8f61 | Exceptional Circumstance 1 - Full year subtotal                                                     | Approved      |
		| Template        | 01dfefd2-5a09-4735-adcc-39a4a23cb482 | Exceptional Circumstances 2 - Full year subtotal                                                    | Approved      |
		| Template        | 597927ef-6020-441a-a8ac-d8517f36cb62 | Exceptional Circumstances 3 - Full year subtotal                                                    | Approved      |
		| Template        | 7a4f3a8a-6553-440a-8616-88d63dba1410 | Exceptional Circumstance 4 - Full year subtotal                                                     | Approved      |
		| Template        | 858ce70f-37a5-47c3-aac2-fd226a5cc1dc | Exceptional Circumstance 5  - Full year subtotal                                                    | Approved      |
		| Template        | a38af01d-625d-445b-8eea-f95b3cff1847 | Exceptional Circumstances 6 - Full year subtotal                                                    | Approved      |
		| Template        | 71de43e4-ec29-4646-ad54-301a6425e286 | Exceptional Circumstance 7 - Full year subtotal                                                     | Approved      |
		| Template        | 1ef435f2-ec31-4f30-9711-42ffe5e91d68 | Prior year adjustment to SBS - Full year subtotal                                                   | Approved      |
		| Template        | 1675164c-0f2c-442e-81c4-a1bd65902f80 | Pupil Led Factors - Full year subtotal                                                              | Approved      |
		| Template        | 50ee9a48-fb65-4d23-a750-94bcfb4c9488 | Other Factors excluding MFL - Full year subtotal                                                    | Approved      |
		| Template        | b759aeda-ef33-4cae-926a-a7dc61e5d58a | Other Factors excluding MFL                                                                         | Approved      |
		| Template        | 2c75243f-0e91-4e0a-94f3-0c7cb0d0c031 | APT MFG Funded Pupil Numbers                                                                        | Approved      |
		| Template        | def15a33-3a0a-4802-949a-54413bf7441e | Original APT MFG adjustment value                                                                   | Approved      |
		| Template        | 3135e370-8684-42e0-aa38-e0667a0cf146 | Current Year MFG after MFG and affordability adjustment                                             | Approved      |
		| Template        | f9084246-144a-48e8-b5a7-747b8dbb0507 | Current Year MFG Unit Value after MFG and affordability adjustment                                  | Approved      |
		| Template        | 973e75bd-6d27-404e-baf0-3e30a568c20e | In year opener overall net adjustment                                                               | Approved      |
		| Template        | 46456238-2f0e-404f-9ae2-39110121e33d | School budget share including MFG                                                                   | Approved      |
		| Template        | cb6ffea4-c6d0-48f8-ac15-6afb8bc92345 | Percentage Change in MFG Unit Value after Adjustment                                                | Approved      |
		| Template        | aae368bc-dd3f-433b-9992-68f4f296538f | Previous Year SUG POG Funding                                                                       | Approved      |
		| Template        | 8372cc73-910c-4fce-8e5f-0033930e14db | LA overall per pupil rate Current year                                                              | Approved      |
		| Template        | 99530590-73a4-4fb4-b4ad-a2e6ba72a9c0 | LA overall per pupil rate Previous year                                                             | Approved      |
		| Template        | 2bb6982f-862d-4c81-b245-cb05777131fa | Funding Protection floor                                                                            | Approved      |
		| Template        | 892813a3-d28f-470e-9bac-71e352ebd0ef | Extent below funding protection floor                                                               | Approved      |
		| Template        | 5bd17eaf-37bf-458c-aa17-79b4eecbc3cf | Percentage change in LA per pupil rates                                                             | Approved      |
		| Template        | b7669ac2-1fde-4e32-8e51-4392f980eb4c | Funding protection Evaluation                                                                       | Approved      |
		| Template        | 7e099955-e274-4f4e-a046-0eb4fd17a2cf | Difference per pupil                                                                                | Approved      |
		| Template        | 35d7efc0-23c2-4afe-8f44-e2be41ae9030 | Free School Protection                                                                              | Approved      |
		| Template        | 49d87089-de9b-4dbe-9ed9-cf0685e5316a | Free school protection - Full year subtotal                                                         | Approved      |
		| Template        | 1c407603-5b6f-4399-bdd2-dcec417d574e | School Budget Share - Full year subtotal                                                            | Approved      |
		| Template        | 5cc20f7f-b6a3-450c-8cbd-67ee9caed7e8 | De-Delegated funding retained by the LA                                                             | Approved      |
		| Template        | 6924a9a7-9375-471a-bae3-8c4f22f658de | De-Delegated funding retained by the LA - Full year subtotal                                        | Approved      |
		| Template        | 854ed3e8-a3e1-4be8-97ed-2d2864a2178b | Total High Needs - Full Year subtotal                                                               | Approved      |
		| Template        | d63850aa-fb2c-4ea3-a1b5-c86e2ae473f8 | Notional SEN funding - Full year subtotal                                                           | Approved      |
		| Template        | 27419dfd-a00c-43c0-9ce3-334d99da0b81 | Funding previously De-Delegated - Full year subtotal                                                | Approved      |
		| Template        | c6f06134-08ef-426b-9a7c-9dd981e80c79 | Funding previously de-delegated                                                                     | Approved      |
		| Template        | 880e79c8-4a01-46d6-abe1-1068fffc68b7 | Funding Basis                                                                                       | Approved      |
		| Template        | 394980d6-c1bc-4885-880c-50a8529c3b2f | Phase of education                                                                                  | Approved      |
		| Template        | b62be92d-b2e2-487f-87cc-0d974F9dd6ee | MFL Adjustment SEN Total                                                                            | Approved      |
		| Template        | 109d4ba4-6cdb-4ea7-841d-78185452cac9 | MFG Overall Net Cash SEN Total                                                                      | Approved      |
		| Template        | 0c2e6487-6dca-4cb3-9C07-2106d240a661 | Total Exceptional Circumstances Full year subtotal                                                  | Approved      |
		| Template        | a955c2da-ad0e-42cd-abf6-0ccbe31c834e | Teachers' pay and pension grant                                                                     | Approved      |
		| Template        | 73bec0b8-7bb2-473e-90e2-3a0238babf04 | Days in the year                                                                                    | Approved      |
		| Template        | d4b63faf-d2ea-40dc-b94a-8b1cf45bd882 | MFG Pupil Adjustment multiplier                                                                     | Approved      |
	And calculations exists
		| Value | Id                                   |
		| 320   | 9ed150c9-072d-4c32-be8a-d0ce83e2dd1a |
		| 320   | 4f6058c6-07e8-4556-8c79-ec703ec55ee7 |
		| 320   | 6c7c925a-fba0-4950-8aa3-5d1bb567081a |
		| 320   | eca30f3a-c262-4435-84a8-13e3e1af67bf |
		| 320   | 60b82ba0-785f-4709-8905-020ec4765391 |
		| 320   | 2e49d940-309f-448c-8e2d-50017ff31db8 |
		| 320   | baca161f-899c-4ab2-80eb-23d6b0bbb56c |
		| 320   | 07a0046c-5e37-4521-86b7-dca803cd24f4 |
		| 320   | f5e5e354-2529-49f3-baf0-d13e4e680097 |
		| 320   | a82e0f02-aaf8-4528-be5b-980ef36c58d5 |
		| 320   | 94b9f8e4-bebe-4831-b9bc-43bc80886d5a |
		| 320   | 6a575190-07d7-41c7-9e8e-7f23c961e75c |
		| 320   | 76fe73f1-59c5-400b-b406-1b3a79cea749 |
		| 320   | 16308fe0-8b90-44b4-aa72-ae53914c5d6c |
		| 320   | 45049cd3-f7ff-4010-8614-5ba4940e4866 |
		| 320   | ccf52cc7-11bf-4552-b7e2-9a4f01f7ecea |
		| 320   | d4832342-6fe3-4f7c-9d01-4958e35e6033 |
		| 320   | 3a99311e-7911-4495-bc3e-b918cf9a2f64 |
		| 320   | a989532b-79fd-4ec2-a578-b212dcdd349d |
		| 320   | c3bc3133-2db7-4b15-83f3-d4e6c29823b3 |
		| 320   | 9340b58d-39c9-4f82-bab6-b2dbaebc2037 |
		| 320   | 84730c25-6d54-4d83-8a35-35635039a0b3 |
		| 320   | d27a0649-383a-42b0-9e9e-20b2e9b66013 |
		| 320   | 249746cc-1a3f-48f0-98a6-1462be3c055f |
		| 320   | 4e9e5f06-3e8a-45b5-9a5e-0e84fd6136b8 |
		| 320   | 46de110c-a53a-40a6-ac3c-25c22933690d |
		| 320   | 27ec0c04-218d-430e-bb2a-4f779e5337c8 |
		| 320   | 439ea611-0fd4-4a8a-9795-3466a2d7d87d |
		| 320   | 36568277-f367-4c09-9bc5-d9d445fc0665 |
		| 320   | c02e8a8a-a81b-4317-be9b-41cfe2621ba7 |
		| 320   | 90aa01f2-aa21-4de7-95c5-8d5b65c0e621 |
		| 320   | 117103ed-e783-4f67-bd67-f1a15622355f |
		| 320   | 57ecf3c7-c4ab-4e55-b8a3-e6929722ae29 |
		| 320   | 8e8c8a46-89d2-4e31-a606-1b33d7e90c69 |
		| 320   | 09917096-d399-484a-af15-82f5375fa6fa |
		| 320   | 3e0a9c97-c89e-4c12-9fee-3a30b87dc2d5 |
		| 320   | 29608864-b47d-461b-9dc5-e6c064d6453c |
		| 320   | 6e661f39-f7aa-44f3-b57e-4a4b2bfa772f |
		| 320   | 7c36427f-b696-4c9b-9646-ace4c7055aaf |
		| 320   | a7308e2c-9e81-44dc-940d-31dcbfea393d |
		| 320   | 1d88ae14-b911-48a9-a25a-78459c199ad9 |
		| 320   | 46a69894-a912-4495-a7fd-cac29fe77e1d |
		| 320   | 69bf74cd-8099-42b7-90a8-ebeecc5151f0 |
		| 320   | 04c5b6e5-b5c9-40c4-a04c-eae91b899c86 |
		| 320   | 058f94c5-4080-49fe-ab87-b92ada5bd86d |
		| 320   | f7c14f5f-fa54-4699-ac16-dbfc76bd0189 |
		| 320   | bd2a3406-38f7-4f55-b951-a6a736728880 |
		| 320   | 250b33cd-4f77-499b-99a8-1bdb1a39d7f8 |
		| 320   | dbffcced-ad33-4ad8-a366-b654261805c2 |
		| 320   | 2ebf2483-71bd-482e-a797-84498d6a7545 |
		| 320   | 44e8b811-ab6a-4efd-9bd7-12c50974320f |
		| 320   | 37a799bc-c8b0-47be-9ef4-7ef33f2adb37 |
		| 320   | a8b1680d-111b-4978-ae66-79a2ff342e18 |
		| 320   | 376decb8-a3d2-4afd-913d-b68a40bc4ce5 |
		| 320   | 3993030d-63fa-48fa-a977-a05a9699321b |
		| 320   | 8b82c0f6-590b-4e4a-a218-74fec8c324a0 |
		| 320   | 17773fc6-0500-42b5-9d99-7b6ea66731cb |
		| 320   | c384271b-66a8-4a7a-8b99-70fb862603ce |
		| 320   | 9c761e8d-724b-4320-b437-99ffc2de1e4f |
		| 320   | 50c7a295-a5f3-4076-b484-0498f3a597d6 |
		| 320   | 24c394d9-172a-43e1-b6b8-ffc8f9eba592 |
		| 320   | fb808f92-8370-4b09-972c-0674472f810a |
		| 320   | f175ab50-f70d-4ef5-a469-d5847e0c5770 |
		| 320   | 212326eb-71ba-474a-93d0-b3f0ff843a74 |
		| 320   | 7bc842e8-3525-4db9-8e3c-788d2a99d5db |
		| 320   | cfe5a04e-fa4a-4124-8df2-a7465b5acbe7 |
		| 320   | 57d3a9d0-318d-4500-b3be-a9eeaf4973e0 |
		| 320   | c064520f-f84f-40e2-8db0-09573581ca40 |
		| 320   | e4e582e9-fc5f-4511-ab29-4d423b30ab2d |
		| 320   | 418d7e1f-f8fe-49f5-b578-03015a5ecb33 |
		| 320   | 3a3c4c11-63c7-4ea2-8731-9aaf324604e9 |
		| 320   | f506ecc9-edce-40f3-8a1b-42f03f36e2e7 |
		| 320   | e3aa6c5f-57d6-4901-8132-5157aad9fb1c |
		| 320   | e9302611-d9c2-429e-b383-a9300004d64a |
		| 320   | 21dcf091-b09b-46c0-b9f5-1f421992f741 |
		| 320   | 31264a49-e83b-4908-8114-11628128194b |
		| 320   | 9f176522-f981-4021-937c-8d6b3dca5f27 |
		| 320   | f82e601f-128d-4a37-bcdb-d1677853e08c |
		| 320   | cb58d29c-f81a-4d89-adba-ff29076bfccb |
		| 320   | 377038dd-e124-4afd-8bdc-e45f39150ebd |
		| 320   | 204e9392-eff0-4433-b81a-31968e3b0a25 |
		| 320   | 3d6c4eb8-2a8a-4db8-86a2-182194f4a419 |
		| 320   | e8d20a05-4a2e-4543-b44a-daef1899e6ea |
		| 320   | cd2c6719-b190-4903-b29d-a7c8ebe9390a |
		| 320   | 35a97833-624b-4589-9327-38a42803996e |
		| 320   | dc370e8b-fc76-44cb-9e08-c4f57b631dcb |
		| 320   | bd794e50-e32f-4075-92cf-53fec4c3a044 |
		| 320   | 50ce2c4c-86b8-4fd5-8fc9-c68aad616f14 |
		| 320   | 358e784b-179b-4eb9-bd72-8d0218466bec |
		| 320   | 54081115-96df-4f67-90aa-da23e1820933 |
		| 320   | b3172c58-c2e3-41e1-ac2a-fd9ca2dde72a |
		| 320   | 2d75f7ca-c68c-4eaa-9c06-bca4802f39fd |
		| 320   | 6ec89196-f0b8-4fa1-8453-3555aeb3ac5d |
		| 320   | fd2cf131-5488-4503-97f5-7697e5078381 |
		| 320   | b8baa2c5-24ab-4499-8a3e-1590bcaa5234 |
		| 320   | 7598ef03-1d86-4981-8a89-f44820fbfdd5 |
		| 320   | 05261e95-bb9c-4f7b-88a1-f313c92d5e50 |
		| 320   | a47c4e52-90f3-4b8d-8635-3a997fdd9660 |
		| 320   | dd6781ef-dc12-483f-910f-7ff0b010e995 |
		| 320   | 1e74f84b-333d-4d0d-be49-9388ca6aa4aa |
		| 320   | 7bd5ea1b-55c2-45fb-9748-2c1dbfa010b1 |
		| 320   | 636c17a8-6b21-4939-8098-e804f2988e75 |
		| 320   | c4c2c50e-f669-46e1-8b55-f6075712c90f |
		| 320   | 41eca670-8db0-435d-8b9a-91567571eb3d |
		| 320   | 2daed5a4-1720-4214-973a-3ab961ca9abc |
		| 320   | 5dbcd2e2-cb73-4941-9e5f-078aeec3feea |
		| 320   | 680881f6-b1c1-492b-8b70-561d28097267 |
		| 320   | a76f7851-062b-40d0-8db8-ce9e64b79538 |
		| 320   | 1e201223-3cb2-4351-8ec3-6ba9dd4850a8 |
		| 320   | f2054f45-cf93-4175-9dd1-c7f15dda5407 |
		| 320   | b6215036-7af8-427d-8e47-231b9f5bc9e0 |
		| 320   | 58183918-c2c4-4af8-9dd7-4afc66f589af |
		| 320   | 477c37d4-fe93-4fb2-9244-613a0c3aebec |
		| 320   | 6d4e439b-fd0d-4968-a77e-b76db92584ae |
		| 320   | a7838e21-fae1-475b-850b-83bfe37e0fb5 |
		| 320   | fcdb6c77-434c-4b49-833b-a2629208adb7 |
		| 320   | 2166eacc-edc9-4bed-a326-c0f4d32117e3 |
		| 320   | fa51564f-914d-4d5a-9024-bf053a9c17c8 |
		| 320   | f13433a0-eea7-41e6-9986-2dcb2bd328a3 |
		| 320   | 9fabe999-c4cc-45d2-b8d7-d2a5cf9f2c09 |
		| 320   | 76fe4544-c380-4e7b-8544-6684f8f133b3 |
		| 320   | b903444e-c0bf-4006-bd00-6d7865b5c1dd |
		| 320   | e3fc8899-a630-431f-a36c-787fd9384826 |
		| 320   | 4094630d-319e-4695-8d2c-d73fa4e696b4 |
		| 320   | 2c0b7bc0-b2b7-4d3e-9685-6deaae56d9f2 |
		| 320   | e420c1d4-ff26-424f-8917-30ea72fdcd73 |
		| 320   | e1178be4-5924-44f7-a8a8-8bb44819c570 |
		| 320   | 058f22ea-ccc7-4f2d-9e2b-fffc6d7fba4b |
		| 320   | a19df2b3-63d8-47d2-9405-c8863c7a34f7 |
		| 320   | 982693f4-c015-463f-ad0c-8c5e9c6ffab8 |
		| 320   | f0ea2e21-1ade-4cb7-84ac-539952c6199c |
		| 320   | c8898af8-188a-4154-809c-e50f6e230025 |
		| 320   | 78183215-4c23-4b4f-a9bd-9e5f8caf61ef |
		| 320   | 77e37a97-32c6-4326-876e-872143251629 |
		| 320   | 1c3df7fb-a773-4f8d-83bb-2e4ab2ae50c4 |
		| 320   | cd841069-71f5-485b-a9fe-0372d3883306 |
		| 320   | 00e22126-05c9-4c3e-a6ff-1187e67f5090 |
		| 320   | e6181805-68ac-480d-89ab-1b9642f6a9be |
		| 320   | 52000633-dc4f-47b1-8fed-af823e2c0ea3 |
		| 320   | d01a0579-a898-4c11-9822-34c21aff3c42 |
		| 320   | fb5770ce-4079-4416-bb08-b9f5ff4ed39f |
		| 320   | a90f3e18-5674-4e11-8301-200a437d2de1 |
		| 320   | af7b5468-275f-472b-a3d8-57bc9deea236 |
		| 320   | 40b62036-9bef-4c12-a5e9-ec0606dc1e23 |
		| 320   | bd86816e-63fd-447c-9975-12b2e1013921 |
		| 320   | 89416254-2575-434b-bd58-ef9296ddff66 |
		| 320   | 33c00845-bbe1-4d16-b191-d9cf7814622b |
		| 320   | c75abe6b-3b8d-4872-ba87-48468bcba427 |
		| 320   | 3417c52b-5fff-4e15-8fce-ba62684c787a |
		| 320   | 0aebb694-f29e-47f7-88d9-5c794a3695dc |
		| 320   | 1f76dae7-d72a-42a0-9ec4-9bb106ae0156 |
		| 320   | 8138e75b-777e-44e8-9609-d3fde44f85b3 |
		| 320   | 67b09d13-ef01-4f8b-82bf-3c83bf0367b9 |
		| 320   | 6454f2d7-49a6-48f6-b4e6-745240bf2ea7 |
		| 320   | 6ca9923c-86ef-4165-8c77-a0998f0a8b48 |
		| 320   | f2a302e2-5cf5-45f6-9944-0809da9e4f78 |
		| 320   | 111200c6-07c8-4424-967b-3b2c3d1d1b0f |
		| 320   | 0eeeab9e-8a5e-40bd-9439-1e22ed6fa211 |
		| 320   | e61cf665-47c0-4149-95fb-0f09de81f97d |
		| 320   | 7e238559-0d24-434c-8edb-c964e5e5ef3b |
		| 320   | 87067f53-7eac-4e86-883f-3db34e5276cf |
		| 320   | 3ba29f60-eb82-433b-bb97-8f10d47e5cc9 |
		| 320   | ac63be86-3b6f-4c77-9b15-e45fe925c9bf |
		| 320   | eb9d407e-24b6-4c4c-9b3a-1e0cf05c6e0c |
		| 320   | 4c9cf10d-6d7c-4e3b-886d-d772b89e2814 |
		| 320   | 62eb6b69-1521-44d5-b58c-c2b3dcfc1e02 |
		| 320   | b5bfef59-1fd8-4475-b815-a7fae50a882c |
		| 320   | 5c88baa2-f663-4982-bd11-488d276b54a9 |
		| 320   | 162d56e6-8b92-46ac-a638-b387d4d79ce1 |
		| 320   | 9a6b6afb-751e-4291-a29f-0081122c647b |
		| 320   | fdfacd09-bebe-4fde-a2d0-a8685fc0ac37 |
		| 320   | a8973266-8d72-45cd-9cb8-e14f0870b57c |
		| 320   | cb8bb595-7cd1-4135-ba02-ed3d1395df27 |
		| 320   | 2afe77ee-8168-4a07-95f1-f7b557206b63 |
		| 320   | 40fbc5d3-6eb9-47f7-b834-91ba43169d18 |
		| 320   | d0d677a4-5f82-4714-81a6-d5f63aa8aa8e |
		| 320   | 3c000f9b-4995-4903-81b0-e4a587f54dd8 |
		| 320   | 3e9d6f38-5098-4793-8268-bcd2c6753830 |
		| 320   | feebb937-77b4-4392-b1c6-f4bef176000c |
		| 320   | 8a73996f-6983-4785-9dd2-3c3706c928b9 |
		| 320   | 2f6a0828-2d2c-4bbf-8bc9-150256f594f1 |
		| 320   | 6bc558ab-b158-44b8-b7f8-7a6befbf89bb |
		| 320   | 5317c77e-07a1-4a62-9109-12ab581101a5 |
		| 320   | ff122d10-4963-4b27-b7b6-99c046d898b4 |
		| 320   | a26fe811-91e0-4b9d-9fe9-613d445ceb77 |
		| 320   | 83945ff2-76f7-4b4a-addc-cd2a13069063 |
		| 320   | 09408dd6-45a1-4ce0-bbb6-119a85e3aff3 |
		| 320   | 186490d0-8f6c-4c0b-ad41-8b06ca2adc74 |
		| 320   | 2a0f0f2d-b945-4e80-97cf-37cba8fa32a5 |
		| 320   | 5f94bc40-debf-49ce-8cb8-f399de31a9fb |
		| 320   | 0f4f02c5-9d56-48f0-9c26-0d89418e5641 |
		| 320   | 433f02eb-4d12-497b-9fb6-67f23daab6b4 |
		| 320   | c496d274-15cb-450e-93aa-ccc1c1d210b1 |
		| 320   | 41fd7633-babd-437d-89a8-7a6756a77463 |
		| 320   | d4b7bedc-98bd-4b9a-9dbc-b6b61ee00625 |
		| 320   | 56ed9478-ad68-4d4d-afcd-ce96f633d963 |
		| 320   | 9016b5de-cd6e-4025-9bae-92cdd16560fd |
		| 320   | d5d1d35c-459a-4036-9589-ad6a4bc96348 |
		| 320   | 3f8a09a0-4fc2-4346-9f34-b26b0d9b8bff |
		| 320   | d4a02087-e3a9-4498-8dc3-b1662a587b60 |
		| 320   | 11fbe321-1b2b-4221-bc18-67ebc07225d3 |
		| 320   | d6be331a-253f-4363-a375-bafca45f4879 |
		| 320   | 58d9d140-5a7b-4039-8877-f0d2aa71159a |
		| 320   | f41ab80c-e39b-43bc-aabd-6aeb70fc38fa |
		| 320   | 0e5c8575-434c-44b3-b62a-52b3cf90cde2 |
		| 320   | 3378a1bb-8a56-4ab1-a608-f118dd32c036 |
		| 320   | 63e772cc-01c4-404e-88c1-e0b0e676ab28 |
		| 320   | 3a2af9de-e573-4e24-b0cd-577ddc231c22 |
		| 320   | cf92aa4f-03f3-4cd3-b086-a12b43039226 |
		| 320   | 38625ca7-076e-4da0-bee9-745306951fd8 |
		| 320   | bb412013-aa9b-4568-8309-d37420932eab |
		| 320   | 1e51c13a-b2a7-462a-b31c-649034f7819e |
		| 320   | 141bfd4a-31a9-41b0-a2d2-bc6237abd590 |
		| 320   | 52262e18-d970-4a63-a022-d6a2cd5b99d0 |
		| 320   | 9a06c82b-80ae-4620-b8bf-1d976b98cfc4 |
		| 320   | 08fa534f-d2bb-409d-9d0b-46ee9891500d |
		| 320   | d3acda35-dde9-4fbc-807e-e24ed3bdb28b |
		| 320   | 2826e609-f187-4912-9a89-23c8c93f98e8 |
		| 320   | 4ce5468d-bda6-4d5a-bae5-d958d4c99508 |
		| 320   | f0951107-9c48-4f37-897c-981dada3efc9 |
		| 320   | 7a4dc0d7-5aa5-4ec4-8e9a-72f5209c5ac7 |
		| 320   | 670cd089-f292-4539-b900-64534f5eccf3 |
		| 320   | ef7b1a48-815d-49b6-9b36-ec604fbfb72b |
		| 320   | c2e0d2f8-2277-4445-a1f9-eca963b78a4c |
		| 320   | 5ad44b8c-97d9-4f8f-927a-d242240b9df7 |
		| 320   | c6d9191a-9268-4246-86d8-596ca99124e8 |
		| 320   | 3e4ff4f7-b78c-4821-b559-40b3ee581e37 |
		| 320   | 919af526-2140-4ce8-a03e-9e478bd7a52e |
		| 320   | ad74e7af-16a3-47f9-9bbc-657f3062293f |
		| 320   | 596f79fa-f67e-4b47-a945-7bd65b223630 |
		| 320   | 2fad3459-ba4a-4eaa-a6e7-d8ba9d38261b |
		| 320   | 3eb7be55-f247-4803-962f-b2c0676581b6 |
		| 320   | 88cbb66c-a32c-47bc-bc4b-9894b72e6691 |
		| 320   | 1a1ba3be-f479-454c-a8d2-a0fe9c99e192 |
		| 320   | 31c653cf-af45-4990-9c91-ace572bbba12 |
		| 320   | 14718456-a287-4722-a56f-60be7916e415 |
		| 320   | 683b3d1e-c656-444f-ae45-837c9756b19e |
		| 320   | 2cfdb755-89c4-49d1-8854-a1aa439e4b44 |
		| 320   | c855e356-54ee-4f2d-a866-95a99c3db76a |
		| 320   | 0f3836da-1b14-49e9-aed9-9a058c04f9c6 |
		| 320   | 12ae9f9a-d9fd-4852-8ed9-114503ba9e6b |
		| 320   | f6281310-00aa-42f0-a4a4-39541c3c4caa |
		| 320   | 55c78b2d-1340-45a5-846f-8d614dc55c92 |
		| 320   | e1a30933-0fad-49ec-beb3-34038d2fb5b2 |
		| 320   | 9a3f57ef-d715-4ce2-bd94-45e03d11cbfa |
		| 320   | a6c8f2f9-1d10-487c-a6bc-d6fda82e3f5c |
		| 320   | 9fcda229-77a0-4578-ae98-af5608d9c654 |
		| 320   | a90800cd-bdcf-425b-8916-1b8649abc8ac |
		| 320   | c9c068a7-142a-457c-8e92-bddd8cc6f8e4 |
		| 320   | 0dd75339-0803-46e5-af5d-2b012d19c4ce |
		| 320   | 8079034a-6c5d-44f0-bd3e-6ead8e94bb56 |
		| 320   | e864becd-892d-4656-912a-48f4802bce76 |
		| 320   | a7fe509d-b9e6-4dc8-a481-7291fb7eab95 |
		| 320   | 9c09a321-eca4-497b-bb41-9667a8d6bcfa |
		| 320   | 72e24f7e-5dcf-4c2e-b88c-4277d8efbad4 |
		| 320   | fe3bcb3b-3998-40bc-91fe-802110ab59d3 |
		| 320   | 38fedaa4-4f2b-435e-a457-0044513ae4d2 |
		| 320   | 0b4a43d4-88f7-47fa-89d5-7e17113739e0 |
		| 320   | cfbb4880-21ee-4265-8918-b61ca48b8af2 |
		| 320   | d7b2b451-4e05-4bd6-9fe2-2fe9d082f1d5 |
		| 320   | 698822c6-ba43-424f-ad6d-726ff81aa95b |
		| 320   | bff2812a-a128-405c-ad87-5a917b986853 |
		| 320   | 174e95d0-11cd-4d56-8d5e-8edcf0faa9f8 |
		| 320   | 0e137c2c-d55e-4f76-ab21-eab66d7b0ba6 |
		| 320   | 31e97413-c3a3-4f91-846d-1481e2ea71f9 |
		| 320   | 1df9c246-57ad-4987-a598-a7c16e528b14 |
		| 320   | 5e4d20e9-ff72-4796-86a2-22f17444dce0 |
		| 320   | b1c698f3-dd53-478b-a488-92fba43236e6 |
		| 320   | 59d5f9df-8082-42ca-8385-0d602322c926 |
		| 320   | 7765d8a9-5c06-42d7-86ae-d43002a73d4c |
		| 320   | 2057be6e-ea03-4262-8c93-076725659d86 |
		| 320   | 7df29588-9a41-4375-b205-530604ada14b |
		| 320   | 4fba8423-b5fe-41e7-96d5-125b23709286 |
		| 320   | 7ea4ea56-7df3-42bd-9561-0f1ca2240328 |
		| 320   | 274a745b-cbf6-4852-a09b-c7c606d92298 |
		| 320   | ca588a55-e282-4ebc-a23f-e4aaff6f17a5 |
		| 320   | 4aef4fa3-eff9-4216-9cb6-d553e163f8fb |
		| 320   | 3e636fc2-10c6-4996-a143-bda19fef4fa8 |
		| 320   | 4ca8af79-2dc4-4a76-94c1-4dd05cde9c81 |
		| 320   | d65aae08-2b13-45a0-985d-0de04b5b5386 |
		| 320   | 7af1f923-4336-466f-bf56-773403c82598 |
		| 320   | 7e282e12-6f81-408e-be6d-ff693f498635 |
		| 320   | 679d4d47-1eaf-4dc0-be20-0370600fefec |
		| 320   | abd5d8cd-2382-4347-9348-062dd27d8906 |
		| 320   | 7832f65c-5be3-4905-9592-029df8f258b4 |
		| 320   | d07eede9-9a1d-46ac-83d2-91671737d41a |
		| 320   | 1110f308-d7f1-4364-b4bd-d0ea050c9864 |
		| 320   | eefd8b04-14a6-49b5-9943-5a10864a7929 |
		| 320   | bd40f3cb-7b04-4aa7-a146-40f40f3a2f64 |
		| 320   | 5151e6c2-6274-4283-ba7d-44c9357612a3 |
		| 320   | 91be94f0-a918-4ac5-9a7e-3426045202b3 |
		| 320   | 287d66e5-350c-445f-bd3b-81153de5501d |
		| 320   | be6a08ac-2103-4fcd-b926-385873fdabe1 |
		| 320   | e9d7c408-31a0-4bd4-a33a-9e35b172bf3a |
		| 320   | 42020112-29ac-4f09-a93f-897c5da99b2d |
		| 320   | 7cf0d5c7-f00b-4943-81d4-3181a449c7fc |
		| 320   | afe4b7fd-1e0c-4e53-95f5-a4389ddbac89 |
		| 320   | 1238c139-e22f-434b-975a-d34697557ab4 |
		| 320   | c1e2f5b5-b7b2-4ef3-8388-0b37ac03eb88 |
		| 320   | cbc46773-412d-4439-a590-56d0e99bcafa |
		| 320   | 67a08e21-95fb-4b94-b2f4-d523228e4849 |
		| 320   | 1423eae9-cec9-4749-a23b-c15ac7f82e8a |
		| 320   | d00a00ce-0a84-431a-9652-965b7dc42a04 |
		| 320   | be54fbce-c20e-41aa-9d5a-f8781cb40718 |
		| 320   | d4a5fc65-ead4-4dc2-b04e-a958eed65d5d |
		| 320   | dd9751cc-af3f-40b6-83d5-c22e2c7f291a |
		| 320   | 535ce184-f3c7-403d-88f4-a59368e4e02c |
		| 320   | 2d67068b-0d4b-41c1-be26-2a8ebde210cb |
		| 320   | e343b36a-4f08-484d-8167-ecc612bd2c56 |
		| 320   | abdf18bb-7302-4adb-ab01-1789435f1f63 |
		| 320   | 266cbd73-06e7-481c-80c1-1491418b5906 |
		| 320   | d14b5ea4-3d0f-4fe1-87d2-5d00954388e9 |
		| 320   | 39878aab-f03c-468d-973d-dcc2ae45a23b |
		| 320   | 51102ce4-dca9-43dc-8ee1-d5d467ea2752 |
		| 320   | 2a9b0f24-2001-4074-b987-dd32fcf610f5 |
		| 320   | 020bf6b3-b537-4a59-a432-9ed366cf43b5 |
		| 320   | 4260a95a-7e89-4646-80ed-7611b9596bf3 |
		| 320   | 77d7c952-5c8f-4951-a1ac-1018263b27bc |
		| 320   | 9dc41696-4c4d-47bd-b8e4-11a012b311fc |
		| 320   | b95f2a8c-2f19-4cfc-adfc-124f828dc4b2 |
		| 320   | 24b29c3d-a165-4c39-a33b-9b5860003ec5 |
		| 320   | f0afc9a0-f6bc-4a19-ae1b-6ad8e8cb0d1b |
		| 320   | b901b173-1cea-471a-9e5d-feb8aed7bddb |
		| 320   | 9c096a86-f468-4787-bf6f-edcd85b78a8c |
		| 320   | 941f848d-0c98-4365-8676-4dbc376b02af |
		| 320   | 4f833685-26ed-4c23-b025-95158146ed39 |
		| 320   | b5889a12-97bd-4a74-aa49-9d2d4471b3bd |
		| 320   | 4b2999f9-e16b-4150-88b0-29ea6ce08afc |
		| 320   | ae768083-e202-48af-93ba-c2ae41f6064a |
		| 320   | 1882032c-da6c-4d3b-b0f2-7789ea527954 |
		| 320   | ffcc3dee-7255-463e-8e14-7bae34283d4f |
		| 320   | b04ae7a0-4783-499b-af58-a7dcf3926e0a |
		| 320   | 12e0caf0-89e6-4e14-bf7e-ee5900233611 |
		| 320   | 8219ab37-2ea3-44bf-acf7-c2e3500ccb20 |
		| 320   | 37df406f-af2d-4c14-b5e9-c737f752e8b0 |
		| 320   | 2174ef51-f916-4354-919a-2e05465fd66c |
		| 320   | 0706444a-261b-4c38-aa24-93c1257d5b01 |
		| 320   | 8a8c6c57-96ff-47c4-b97f-219c76989251 |
		| 320   | aff5b63f-53c0-45d2-a054-29275219adfe |
		| 320   | 50ac16e0-2820-430d-817a-9ea39552d1b6 |
		| 320   | fa52c84c-84b4-4027-a4ec-365810e9dc0f |
		| 320   | 41eddf91-3af1-4ccc-aac3-6e8810785ee4 |
		| 320   | 630f6425-56d3-45f6-8b74-3c51e154f9a6 |
		| 320   | 21881ce6-13c0-4112-aa06-a65ed54288e6 |
		| 320   | 3ec8ff2e-fd60-40ca-84dc-9d45821bd845 |
		| 320   | ad49eea6-3370-448a-8730-1f8884338956 |
		| 320   | ed5cff68-0b99-49f1-8205-0826bede11f6 |
		| 320   | d15afb54-3321-4820-9410-c1adf37f6ca6 |
		| 320   | 82588b02-5fa5-4b50-8455-e91bbdea72ae |
		| 320   | 9c3d1b02-c585-4b43-a09c-90d6339b8f61 |
		| 320   | 01dfefd2-5a09-4735-adcc-39a4a23cb482 |
		| 320   | 597927ef-6020-441a-a8ac-d8517f36cb62 |
		| 320   | 7a4f3a8a-6553-440a-8616-88d63dba1410 |
		| 320   | 858ce70f-37a5-47c3-aac2-fd226a5cc1dc |
		| 320   | a38af01d-625d-445b-8eea-f95b3cff1847 |
		| 320   | 71de43e4-ec29-4646-ad54-301a6425e286 |
		| 320   | 1ef435f2-ec31-4f30-9711-42ffe5e91d68 |
		| 320   | 1675164c-0f2c-442e-81c4-a1bd65902f80 |
		| 320   | 50ee9a48-fb65-4d23-a750-94bcfb4c9488 |
		| 320   | b759aeda-ef33-4cae-926a-a7dc61e5d58a |
		| 320   | 2c75243f-0e91-4e0a-94f3-0c7cb0d0c031 |
		| 320   | def15a33-3a0a-4802-949a-54413bf7441e |
		| 320   | 3135e370-8684-42e0-aa38-e0667a0cf146 |
		| 320   | f9084246-144a-48e8-b5a7-747b8dbb0507 |
		| 320   | 973e75bd-6d27-404e-baf0-3e30a568c20e |
		| 320   | 46456238-2f0e-404f-9ae2-39110121e33d |
		| 320   | cb6ffea4-c6d0-48f8-ac15-6afb8bc92345 |
		| 320   | aae368bc-dd3f-433b-9992-68f4f296538f |
		| 320   | 8372cc73-910c-4fce-8e5f-0033930e14db |
		| 320   | 99530590-73a4-4fb4-b4ad-a2e6ba72a9c0 |
		| 320   | 2bb6982f-862d-4c81-b245-cb05777131fa |
		| 320   | 892813a3-d28f-470e-9bac-71e352ebd0ef |
		| 320   | 5bd17eaf-37bf-458c-aa17-79b4eecbc3cf |
		| 320   | b7669ac2-1fde-4e32-8e51-4392f980eb4c |
		| 320   | 7e099955-e274-4f4e-a046-0eb4fd17a2cf |
		| 320   | 35d7efc0-23c2-4afe-8f44-e2be41ae9030 |
		| 320   | 49d87089-de9b-4dbe-9ed9-cf0685e5316a |
		| 320   | 1c407603-5b6f-4399-bdd2-dcec417d574e |
		| 320   | 5cc20f7f-b6a3-450c-8cbd-67ee9caed7e8 |
		| 320   | 6924a9a7-9375-471a-bae3-8c4f22f658de |
		| 320   | 854ed3e8-a3e1-4be8-97ed-2d2864a2178b |
		| 320   | d63850aa-fb2c-4ea3-a1b5-c86e2ae473f8 |
		| 320   | 27419dfd-a00c-43c0-9ce3-334d99da0b81 |
		| 320   | c6f06134-08ef-426b-9a7c-9dd981e80c79 |
		| 320   | 880e79c8-4a01-46d6-abe1-1068fffc68b7 |
		| 320   | 394980d6-c1bc-4885-880c-50a8529c3b2f |
		| 320   | b62be92d-b2e2-487f-87cc-0d974F9dd6ee |
		| 320   | 109d4ba4-6cdb-4ea7-841d-78185452cac9 |
		| 320   | 0c2e6487-6dca-4cb3-9C07-2106d240a661 |
		| 320   | a955c2da-ad0e-42cd-abf6-0ccbe31c834e |
		| 320   | 73bec0b8-7bb2-473e-90e2-3a0238babf04 |
		| 320   | d4b63faf-d2ea-40dc-b94a-8b1cf45bd882 |
	And the following distribution periods exist
		| DistributionPeriodId | Value |
		| AC-1920              | 1200  |
		| AC-2021              | 2000  |
	And the following profiles exist
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AC-1920              | CalendarMonth | October   | 1920 | 1          | 1200          |
		| AC-2021              | CalendarMonth | April     | 2021 | 1          | 2000          |
	And the following profile pattern exists
		| FundingLineId	|FundingStreamId | FundingPeriodId |
		| GAG-001		|GAG             | AC-2021         |
	When funding is refreshed
	Then the following published provider ids are upserted
		| PublishedProviderId                                           | Status  |
		| publishedprovider-1000000-<FundingPeriodId>-<FundingStreamId> | Updated |
		| publishedprovider-1000002-<FundingPeriodId>-<FundingStreamId> | Draft   |

	Examples:
		| FundingStreamId | FundingPeriodId | FundingPeriodName               | TemplateVersion | ProviderVersionId |
		| GAG             | AC-2021         | Academies Academic Year 2020-21 | 1.2             | GAG-providers-1.0 |