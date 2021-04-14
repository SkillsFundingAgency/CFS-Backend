Feature: Refresh1619
	In order to refresh funding for GAG
	As a funding approver
	I want to refresh funding for all approved providers within a specification

Scenario Outline: Successful refresh of funding
	Given a funding configuration exists for funding stream '<FundingStreamId>' in funding period '<FundingPeriodId>'
		| Field                     | Value                     |
		| DefaultTemplateVersion    | 1.2                       |
		| PaymentOrganisationSource | PaymentOrganisationFields |
	And the funding configuration has the following organisation group
		| Field                     | Value             |
		| GroupTypeIdentifier       | UKPRN             |
		| GroupingReason            | Contracting       |
		| GroupTypeClassification   | LegalEntity       |
		| OrganisationGroupTypeCode | LocalAuthoritySsf |
	And the funding configuration has the following provider type matches
		| ProviderType | ProviderSubtype |
		| Schoo        | 08SSF           |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value          |
		| GroupTypeIdentifier       | UKPRN          |
		| GroupingReason            | Contracting    |
		| GroupTypeClassification   | LegalEntity    |
		| OrganisationGroupTypeCode | LocalAuthority |
	And the funding configuration has the following provider type matches
		| ProviderType | ProviderSubtype |
		| Local        | 10LAU           |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value                |
		| GroupTypeIdentifier       | LACode               |
		| GroupingReason            | Information          |
		| GroupTypeClassification   | GeographicalBoundary |
		| OrganisationGroupTypeCode | LocalAuthority       |
	And the funding configuration has the following provider type matches
		| ProviderType | ProviderSubtype |
		| Local        | 10LAU           |
		| Furth        | 22OTH           |
		| Schoo        | 08SSF           |
		| Acade        | 11ACA           |
		| Acade        | FSAP            |
		| Acade        | 17NMF           |
		| Acade        | 12FSC           |
		| Acade        | 13SSA           |
		| Acade        | 19FSS           |
		| Acade        | 15UTC           |
		| Acade        | 14CTC           |
		| Acade        | FS1619          |
		| Acade        | 22AAP           |
		| Furth        | 01GFE           |
		| Furth        | 02IPP           |
		| Furth        | 18ISP           |
		| Furth        | 03SFC           |
		| Furth        | 04AHC           |
		| Furth        | 07HEP           |
		| Furth        | 05ADC           |
		| Furth        | 06SDC           |
		| N1618        | 16NPF           |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value        |
		| GroupTypeIdentifier       | UKPRN        |
		| GroupingReason            | Payment      |
		| GroupTypeClassification   | LegalEntity  |
		| OrganisationGroupTypeCode | AcademyTrust |
	And the funding configuration has the following provider type matches
		| ProviderType | ProviderSubtype |
		| Acade        | 11ACA           |
		| Acade        | FSAP            |
		| Acade        | 17NMF           |
		| Acade        | 12FSC           |
		| Acade        | 13SSA           |
		| Acade        | 19FSS           |
		| Acade        | 15UTC           |
		| Acade        | 14CTC           |
		| Acade        | FS1619          |
		| Acade        | 22AAP           |
	And the funding configuration is available in the policies repository
	And the funding configuration has the following organisation group
		| Field                     | Value       |
		| GroupTypeIdentifier       | UKPRN       |
		| GroupingReason            | Contracting |
		| GroupTypeClassification   | LegalEntity |
		| OrganisationGroupTypeCode | Provider    |
	And the funding configuration has the following provider type matches
		| ProviderType | ProviderSubtype |
		| Furth        | 01GFE           |
		| Furth        | 02IPP           |
		| Furth        | 18ISP           |
		| Furth        | 03SFC           |
		| Furth        | 04AHC           |
		| Furth        | 07HEP           |
		| Furth        | 05ADC           |
		| Furth        | 06SDC           |
		| N1618        | 16NPF           |
		| Furth        | 22OTH           |
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
		| 1619 | <FundingStreamId> |
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
		| Field             | Value                 |
		| ProviderVersionId | <ProviderVersionId>   |
		| VersionType       | Custom                |
		| Name              | 1619 Provider Version |
		| Description       | Acceptance Tests      |
		| Version           | 1                     |
		| TargetDate        | 2019-12-12 00:00      |
		| FundingStream     | <FundingStreamId>     |
		| Created           | 2019-12-11 00:00      |
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000000           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Approved          |
		| TotalFunding    | 566380.82         |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	# Maintained schools - PublishedProviders
	And the Published Provider has the following funding lines
		| Name                                                                                 | FundingLineCode | Value     | TemplateLineId | Type        |
		| Total Funding Allocation                                                             |                 | 566380.82 | 0              | Information |
		| Programme Funding                                                                    | 1619-001        | 528304.24 | 1              | Payment     |
		| Retention Factor Funding                                                             |                 | -4006.55  | 108            | Information |
		| Programme Cost Weighting Funding                                                     |                 | 16697.84  | 109            | Information |
		| Level 3 Maths And English Funding                                                    |                 | 3564      | 110            | Information |
		| Student Funding                                                                      |                 | 500818.22 | 12             | Information |
		| Level 3 Maths And English One Year Funding                                           |                 | 0         | 122            | Information |
		| Level 3 Maths And English Two Year Funding                                           |                 | 3564      | 123            | Information |
		| Total Mainstream Bands Funding                                                       |                 | 500818.22 | 13             | Information |
		| Disadvantage Funding                                                                 |                 | 6000      | 134            | Information |
		| Disadvantage Block 1 And Block 2 Total                                               |                 | 4562.5    | 136            | Information |
		| Disadvantage Block 1 Total Funding                                                   |                 | 0         | 137            | Information |
		| Economic Deprivation Funding                                                         |                 | 0         | 138            | Information |
		| Total T Level Bands Funding                                                          |                 | 0         | 14             | Information |
		| Care Leaver Funding                                                                  |                 | 0         | 144            | Information |
		| Disadvantage Block 2 Total Funding                                                   |                 | 4562.5    | 148            | Information |
		| Disadvantage Block 2 Lower Funding                                                   |                 | 0         | 149            | Information |
		| Band 5 Student Funding                                                               |                 | 492608.32 | 15             | Information |
		| Disadvantage Block 2 FTE Funding                                                     |                 | 0         | 150            | Information |
		| Disadvantage Block 2 T Level Funding                                                 |                 | 0         | 151            | Information |
		| Band 4 Student Funding                                                               |                 | 8209.9    | 16             | Information |
		| Minimum Top Up Funding                                                               |                 | 1437.5    | 168            | Information |
		| Band 3 Student Funding                                                               |                 | 0         | 17             | Information |
		| Total Large Programme Funding                                                        |                 | 0         | 173            | Information |
		| Large Programme Funding Uplift At 20 Percent National Rate                           |                 | 0         | 174            | Information |
		| Large Programme Funding Uplift At 10 Percent National Rate                           |                 | 0         | 179            | Information |
		| Band 2 Student Funding                                                               |                 | 0         | 18             | Information |
		| Programme Funding Without Area Cost Applied                                          |                 | 523073.51 | 184            | Information |
		| Band 1 FTE Student Funding                                                           |                 | 0         | 19             | Information |
		| Total Care Standards Funding                                                         |                 | 0         | 2              | Information |
		| T Level Band 9 Funding                                                               |                 | 0         | 21             | Information |
		| T Level Band 8 Funding                                                               |                 | 0         | 22             | Information |
		| T Level Band 7 Funding                                                               |                 | 0         | 23             | Information |
		| Area Cost Allowance Adjustment                                                       |                 | 5230.74   | 236            | Information |
		| T Level B and 6 Funding                                                              |                 | 0         | 24             | Information |
		| Student Financial Support Funding                                                    |                 | 15476.57  | 241            | Information |
		| Discretionary Bursary Fund Total                                                     | 1619-002        | 15476.57  | 242            | Payment     |
		| Discretionary Bursary Fund                                                           |                 | 15476.57  | 243            | Information |
		| Exceptional Adjustment To Discretionary Bursary Fund                                 |                 | 0         | 244            | Information |
		| Financial Disadvantage Funding                                                       |                 | 0         | 246            | Information |
		| Student Costs Travel Funding                                                         |                 | 15476.57  | 247            | Information |
		| Student Costs Industry Placement Funding                                             |                 | 0         | 248            | Information |
		| Bursary Adjustment In Respect Of FreeMeals                                           |                 | 0         | 249            | Information |
		| Residential Funding Total                                                            |                 | 0         | 268            | Information |
		| Total Free Meals Funding                                                             | 1619-005        | 0         | 271            | Payment     |
		| Free Meals Administration                                                            |                 | 0         | 272            | Information |
		| Free Meals Total Including Exception                                                 |                 | 0         | 276            | Information |
		| Free Meals Higher Funding                                                            |                 | 0         | 277            | Information |
		| Free Meals Lower Funding                                                             |                 | 0         | 278            | Information |
		| Free Meals FTE Funding                                                               |                 | 0         | 279            | Information |
		| Free Meals Exceptional Adjustment                                                    |                 | 0         | 287            | Information |
		| High Needs Element 2 Student Funding                                                 | 1619-006        | 0         | 299            | Payment     |
		| Industry Placements Funding                                                          | 1619-007        | 0         | 300            | Payment     |
		| Advanced Maths Premium Funding                                                       | 1619-008        | 11400     | 301            | Payment     |
		| High Value Courses Premium Funding                                                   | 1619-009        | 11200     | 302            | Payment     |
		| Teachers Pension Scheme Grant                                                        | 1619-010        | 0         | 303            | Payment     |
		| Alternative Completions Funding                                                      |                 | 0         | 304            | Information |
		| Residential Bursary Fund                                                             | 1619-003        | 0         | 309            | Payment     |
		| Residential Support Scheme                                                           | 1619-004        | 0         | 310            | Payment     |
		| Industry Placements T Level Funding                                                  |                 | 0         | 318            | Information |
		| Disadvantage Accommodation Costs                                                     | 1619-012        | 0         | 353            | Payment     |
		| Disadvantage Block 2 Higher Funding                                                  |                 | 4562.5    | 373            | Information |
		| Cummulative Programme Funding Without Total Large Programme And Disadvantage Funding |                 | 517073.51 | 381            | Information |
		| Cummulative Programme Funding With Disadvantage Funding                              |                 | 523073.51 | 382            | Information |
		| Total Start Up And Post Opening Grant                                                |                 | 0         | 389            | Information |
		| Post Opening Grant Per Pupil Resources                                               | 1619-017        | 0         | 390            | Payment     |
		| Post Opening Grant - Leadership Diseconomies                                         | 1619-018        | 0         | 391            | Payment     |
		| Maths Top Up                                                                         | 1619-019        | 0         | 394            | Payment     |
		| Industry Placements Capacity And Delivery Funding                                    |                 | 0         | 396            | Information |
		| Programme Funding Without Care Standards Or Condition Of Funding Adjustment          |                 | 528304.24 | 4              | Information |
		| Discretionary Bursary Transition Adjustment                                          |                 | 0         | 408            | Information |
		| Alternative Completions Sporting Excellence                                          | 1619-011        | 0         | 410            | Payment     |
		| Alternative Completions - Sea Fishing                                                | 1619-020        | 0         | 412            | Payment     |
		| Start Up Grant Part A                                                                | 1619-022        | 0         | 419            | Payment     |
		| Start Up Grant Part B                                                                | 1619-023        | 0         | 420            | Payment     |
		| Offset High Value Courses For School And College Leavers In Year Programme Funding   |                 | 0         | 423            | Information |
		| High Value Courses For School And College Leavers Total                              | 1619-024        | 0         | 425            | Payment     |
		| Total Funding Allocation And LAMSS Bursary Funding                                   |                 | 566380.82 | 438            | Information |
		| LA Maintained Special School Bursary Funding                                         | 1619-013        | 0         | 439            | Payment     |
		| Condition Of Funding Adjustment                                                      |                 | 0         | 5              | Information |
		| Care Standards Student Funding                                                       |                 | 0         | 6              | Information |
		| Care Standards Institution Lump Sum Funding                                          |                 | 0         | 7              | Information |
	And the Published Provider has the following distribution period for funding line '1619-001'
		| DistributionPeriodId | Value     |
		| AS-1920              | 5000      |
		| AS-2021              | 566380.82 |
	And the Published Providers distribution period has the following profiles for funding line '1619-001'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AS-1920              | CalendarMonth | October   | 1920 | 1          | 5000          |
		| AS-2021              | CalendarMonth | April     | 2021 | 1          | 566380.82     |
	And template mapping exists
		| EntityType  | CalculationId                        | TemplateId | Name                                                                                         |
		| Calculation | 553a3ad7-a41a-4dc7-95e2-de31a59ce28d | 11         | Care Standards Eligible Students                                                             |
		| Calculation | 7528010c-3fcb-4de6-beaf-d347dbb8a9b1 | 372        | Care standards funding rate                                                                  |
		| Calculation | 19dcc38f-592c-4f53-a546-1e4b61ef8df1 | 9          | Care Standards Student funding                                                               |
		| Calculation | 2401d781-458c-456a-8ca8-ce8541c85c67 | 10         | Care Standards Lump Sum                                                                      |
		| Calculation | 1c9abadf-5c56-4e23-9ee7-225e6879e505 | 176        | Students meeting programme uplift 20% criteria                                               |
		| Calculation | 85d61ce7-e406-4eb0-a21f-367c1c5bc4e6 | 177        | Uplift per student at 20% national rate                                                      |
		| Calculation | af9884e3-44d8-487c-92bc-0e6f79ea1db9 | 178        | Length of programme                                                                          |
		| Calculation | 9cfc3fdf-d785-45ef-a1b6-364b5d1f3444 | 175        | Large programme funding uplift at 20% National rate                                          |
		| Calculation | 703a342f-70c2-4bcf-bed4-a3b83e1980ac | 181        | Students meeting programme uplift 10% criteria                                               |
		| Calculation | 99947b7d-4e46-43ab-829f-9fae386970c6 | 182        | Uplift per student at 10% national rate                                                      |
		| Calculation | bc9e531f-0c47-4457-8771-8316c70993e9 | 180        | Large programme funding uplift at 10% National rate                                          |
		| Calculation | 6157c1a7-cf0c-46f0-9ff2-21c8a85835b8 | 53         | Students R46 total for RATIO                                                                 |
		| Calculation | ac4fdeac-5874-4b48-b2ed-d95ebf66b8cf | 49         | Students R04 Total for RATIO                                                                 |
		| Calculation | d3b4a791-a6fe-4ae4-b1a0-334bad7638e8 | 51         | R46 to R14 Ratio                                                                             |
		| Calculation | 18c4958a-03b5-4bad-8100-f3cb08c57c23 | 48         | R04 to R14 Ratio                                                                             |
		| Calculation | 56641418-45fa-4417-ad15-2c266d526d2a | 433        | Ratio Exception                                                                              |
		| Calculation | 841be5a6-8184-4d81-878d-e53a9fdfd851 | 45         | ILR R14 Full Year Students                                                                   |
		| Calculation | e39a3f89-c5f9-4eb2-8069-7532b8cb19ba | 58         | ILR Rolling 12 months Students                                                               |
		| Calculation | 41c36a55-0702-47fe-bdc3-14e102ed10a5 | 57         | ILR Hybrid Students                                                                          |
		| Calculation | 523063b2-56fb-491e-b433-2cbbc8dc2f66 | 55         | Autumn Census                                                                                |
		| Calculation | 03b1b69d-62e9-4465-aafb-8386586b61ea | 54         | R46 Students                                                                                 |
		| Calculation | de27e3f0-ccf1-458e-ae87-44dac0868c6f | 46         | R04 Students                                                                                 |
		| Calculation | eadf49dc-c4b0-47fb-a0f6-b069446b61e5 | 385        | Ratio                                                                                        |
		| Calculation | 1c3dcf69-6f11-439c-b5b4-2f7a36c051c3 | 418        | R06 Students                                                                                 |
		| Calculation | 9890b97b-4241-449a-9d50-31e4fab8d9be | 384        | Academy Estimates                                                                            |
		| Calculation | 1c864d19-fb3b-4497-bc07-416d98438732 | 59         | Year 1 Business Case Students                                                                |
		| Calculation | 5a758ba8-f24b-4d44-92a5-214487325e73 | 60         | Year 2 Business Case Students                                                                |
		| Calculation | a7c3d41f-56d5-44ac-bf99-d80537c6f70a | 386        | Lagged students                                                                              |
		| Calculation | 1ee652fa-1f8b-48f2-81b7-7f66fd05e1f8 | 44         | Student Number Methodology Used                                                              |
		| Calculation | d3cedf1c-5c3e-45db-b9ae-582eca959ce6 | 47         | Total Baseline (lagged) Student Number                                                       |
		| Calculation | 3aa8c250-13ab-45a2-bf8f-8bb9895d5611 | 56         | Exceptional Variations to Baseline Student Number                                            |
		| Calculation | 689900d0-7e65-4092-8e9a-aca6e8f6469e | 39         | Band 4a students full year data                                                              |
		| Calculation | af0a982b-519f-4bce-b422-73d7493fdaf8 | 40         | Band 4b students full year data                                                              |
		| Calculation | 2dad9dca-4c9e-48c5-97d5-0fbfbb526cbf | 35         | Band 5 students full year data                                                               |
		| Calculation | f8be8202-09b6-43a1-8b26-4b0352f559c9 | 38         | Total Band 4 students full year data                                                         |
		| Calculation | f33a870c-1723-4620-b961-6bf919b83219 | 41         | Band 3 students full year data                                                               |
		| Calculation | fdf310a4-97d4-4fa7-839c-7f85d41c2eb4 | 42         | Band 2 students full year data                                                               |
		| Calculation | f92211c7-2a92-4372-bfdd-2c98857c3809 | 43         | Band 1 students full year data                                                               |
		| Calculation | 13d185c1-d927-4898-9334-9ab07a50b181 | 36         | Total students full year data                                                                |
		| Calculation | fa729035-54ca-4c47-8ce9-b4c9613a3382 | 33         | Total Funded Student number or Academy Estimates                                             |
		| Calculation | f5b823a2-3492-44ae-8e7c-cfce96ad4c64 | 34         | Band 5 Student proportion                                                                    |
		| Calculation | b81eae80-560c-44fd-a330-7f3ef94e0a9f | 95         | Band 9 Funded students current year                                                          |
		| Calculation | 683c125a-3b98-40ab-b667-a11ad4b17919 | 98         | Band 8 Funded students current year                                                          |
		| Calculation | ed30f797-2bc6-412c-a0c7-01a79f33c753 | 102        | Band 7 Funded students current year                                                          |
		| Calculation | 44f6c2bc-9e2c-4794-9751-67480cabed62 | 105        | Band 6 Funded students current year                                                          |
		| Calculation | cb06f25c-c0f0-4af1-9d6a-e06607e81b65 | 30         | Band 5 Funded Students                                                                       |
		| Calculation | ec48dff0-7598-4bcc-bce4-afd916438b58 | 25         | Total T-Level Funded students                                                                |
		| Calculation | 0389cb82-f910-468f-8421-c962ad47b012 | 27         | Band 5 students less T-Level students                                                        |
		| Calculation | 89632558-d0c2-4b3b-9d22-6031e75d81a6 | 28         | Band 5 Rate current year                                                                     |
		| Calculation | abcd282c-d508-4919-ad57-faffaf6de204 | 26         | Band 5 Student funding                                                                       |
		| Calculation | 2e5ec4f8-26ca-48ef-9777-dfe8221ce042 | 69         | Band 4a student proportion                                                                   |
		| Calculation | 7a5ed641-41e3-439e-8c65-fedb67cb500d | 72         | Band 4b student proportion                                                                   |
		| Calculation | 9ba52699-283f-4ea8-bcda-c571399a6778 | 77         | Band 4 total proportion                                                                      |
		| Calculation | c967dd74-156c-495e-ae0e-b519bed7d164 | 73         | Band 4 Funded Students                                                                       |
		| Calculation | 987b226a-d1dc-4b79-ab37-f4613fa4dc9e | 68         | Band 4 rate current year                                                                     |
		| Calculation | ab823493-460a-43a5-a6d7-e489ae35b11f | 432        | Band 4 Student Funding                                                                       |
		| Calculation | be1ed4a6-676e-4b0b-ab41-f14270b2a19d | 81         | Band 3 student proportion                                                                    |
		| Calculation | e0ab7d11-b550-42a7-a2a0-9d5bb76df796 | 80         | Band 3 Funded students                                                                       |
		| Calculation | 7adf1bdc-cec2-4452-b0a4-a95a9735a82f | 82         | Band 3 rate current year                                                                     |
		| Calculation | 31a31d04-64e6-4dec-8482-c3d24c9c7565 | 78         | Band 3 Student funding                                                                       |
		| Calculation | 745f0997-c675-4bc9-be95-31953c5798e7 | 86         | Band 2 student proportion                                                                    |
		| Calculation | a1e3b3e7-de49-43ff-89a7-932a8c13ccbc | 84         | Band 2 Funded students                                                                       |
		| Calculation | 49471705-7aaf-4308-9a2b-ef316cead192 | 85         | Band 2 rate current year                                                                     |
		| Calculation | 72a8ac10-bc57-4057-8907-a0dbda260f5c | 83         | Band 2 Student funding                                                                       |
		| Calculation | 6024bf9d-246a-4d54-ad0e-94ff38ec22bd | 90         | Band 1 FTE full year data                                                                    |
		| Calculation | ea82c1ae-aa28-4a14-b5c9-e5aa7dd96100 | 88         | Band 1 FTE Funded students                                                                   |
		| Calculation | 9895b991-9933-40be-93bb-5f77b89ee5ba | 93         | Band 1 Student proportion                                                                    |
		| Calculation | f3332d77-a051-40ba-a883-f0fc134713ce | 87         | Band 1 FTE Student Funding                                                                   |
		| Calculation | 2a81fe81-18f3-4d11-bab0-8c0a746e279a | 92         | Band 1 Funded Students                                                                       |
		| Calculation | dc5b2ffe-8d48-452d-87bc-29e483b86b64 | 96         | Band 9 Rate current year                                                                     |
		| Calculation | f9c11de5-9e6e-4b73-9986-ab067a7bc0ab | 94         | T-Level Band 9 Student funding                                                               |
		| Calculation | 6c3f69c1-9596-49c7-af84-cb4451033a79 | 99         | Band 8 Rate current year                                                                     |
		| Calculation | 66f880f2-9ee1-4f42-a8fe-3d1f3589351e | 97         | T-Level Band 8 Student funding                                                               |
		| Calculation | 5acf6d2f-b1c0-4dae-a170-8ebf1aae6097 | 103        | Band 7 Rate current year                                                                     |
		| Calculation | de4925bc-b320-42b5-909a-f545450ef16b | 101        | T-Level Band 7 Student funding                                                               |
		| Calculation | 0d3756e6-288b-4891-aaef-a79cf57b48b9 | 106        | Band 6 Rate current year                                                                     |
		| Calculation | d53204a5-cd76-45f2-8bcb-a6a078a788cc | 104        | T-Level Band 6 Student funding                                                               |
		| Calculation | 3a88a844-f40e-4f55-b0c2-0079c0a5f064 | 107        | Total Mainstream band funded students                                                        |
		| Calculation | ca229fec-a815-417c-b680-8dfa0bc7fff3 | 127        | Level 3 Maths and English One Year instances per student                                     |
		| Calculation | 5cee8980-d752-4c03-9cde-1aa1409195fa | 129        | Total Student Funded students                                                                |
		| Calculation | 2ac24e50-4203-4c23-b510-94dea87d42e8 | 125        | Level 3 Maths and English One Year number of instances                                       |
		| Calculation | af4934a6-19db-4dae-bfcb-ea395f9d4ef1 | 126        | Level 3 Maths and English One Year Rate                                                      |
		| Calculation | 3018962a-6355-4dbb-aa99-e141e4763a51 | 124        | Level 3 Maths and English One Year Funding Total                                             |
		| Calculation | 95583d82-0803-4797-9cca-7ae036f48924 | 133        | Level 3 Maths and English Two Year instances per student                                     |
		| Calculation | 60441015-d128-4e8c-a442-5603571d7d9a | 131        | Level 3 Maths and English Two Year number of instances                                       |
		| Calculation | 57506711-b246-446a-bd21-34a091a81bcf | 132        | Level 3 Maths and English Two Year Rate                                                      |
		| Calculation | 90c4af52-b6ff-4a32-a4ba-7de2aa259873 | 130        | Level 3 Maths and English Two Year Funding Total                                             |
		| Calculation | 436e47cd-4cc4-4f13-a0ca-8b5ca5b580d9 | 113        | Student Funding                                                                              |
		| Calculation | 9d75300c-c3fd-45f1-9aaf-329e4a656d1d | 116        | Retention Factor                                                                             |
		| Calculation | d75578b7-e178-4b6e-8339-63693af83d4e | 114        | Student Funding with retention factor applied                                                |
		| Calculation | 0f26909b-e18a-408a-95bd-284d40012cd4 | 111        | Retention Factor Adjustment                                                                  |
		| Calculation | b945fe90-d97a-4d5c-a356-04eba8f64593 | 121        | Programme Cost Weighting                                                                     |
		| Calculation | fc28a81d-ac92-44e4-9019-749d0bbb3537 | 118        | Student funding with retention factor and programme cost weighting applied                   |
		| Calculation | a0c9f82b-d505-4706-b268-17eca2cc57c2 | 117        | Programme cost weighting funding adjustment                                                  |
		| Calculation | b4bb26dd-e7a5-4237-a788-b899848a18cb | 141        | Student funding with retention programme cost weighting and L3 Maths and English             |
		| Calculation | da4a1622-3ee1-4da2-b234-399a7f670f99 | 143        | Block 1 Factor                                                                               |
		| Calculation | a6747aa9-0c9f-484b-8014-27d95e5c1afd | 139        | Block 1 Funding payment                                                                      |
		| Calculation | 80441694-000f-4e73-8c93-5916a9a3c86d | 146        | Number of qualifying care leaver students                                                    |
		| Calculation | 043f26b9-04c4-44ab-8f30-02ad59651c3c | 147        | Care leaver rate per qualifying student                                                      |
		| Calculation | ebb3445a-5273-4062-87ae-ecc415060e84 | 145        | Care leaver funding                                                                          |
		| Calculation | 83f74d71-3681-4931-9dfb-e55c3b807c47 | 158        | Instances per student                                                                        |
		| Calculation | 540e3af3-5913-4124-a4bf-a7112f6c2ceb | 156        | Total funded instances current year                                                          |
		| Calculation | 25643e61-1bd2-46cf-9ee6-85214cde47bc | 154        | Student instances attracting the lower rate                                                  |
		| Calculation | bc9af8f8-1cab-4860-8f0f-3460dae55105 | 155        | Disadvantage Block 2 funding lower rate                                                      |
		| Calculation | 67e6521a-42e4-4226-a3c7-5033e4861a54 | 153        | Disadvantage Block 2 lower funding                                                           |
		| Calculation | 2f011352-0a2f-4ce6-9a16-7014a1037bd3 | 164        | Students attracting the disadvantage block 2 FTE Rate                                        |
		| Calculation | 0324416b-2724-4996-8eb1-a04f799ec1d9 | 376        | Disadvantage block 2 higher rate                                                             |
		| Calculation | f224bbde-85f8-4981-9908-d01e8fb62787 | 161        | Band 1 Funded student instances                                                              |
		| Calculation | 0debb6c2-f189-40c9-8420-b43147a7bdb3 | 162        | Disadvantage Block 2 FTE Funding                                                             |
		| Calculation | 2cf180d6-94b6-436c-b20d-ab458b772db0 | 166        | Disadvantage Block 2 T-Level rate                                                            |
		| Calculation | 4cd7f32e-16cf-41a0-b766-648f6f686eed | 167        | Students attracting the T-Level rate                                                         |
		| Calculation | 67974b2c-7068-4920-a8de-632b7f428148 | 165        | Disadvantage Block 2 T-Level Funding                                                         |
		| Calculation | eeb98d36-dea1-4fa4-a692-67c1c218c82b | 375        | Students attracting disadvantage block 2 higher rate                                         |
		| Calculation | b16e19ee-352b-4959-978c-0789f0530c0e | 374        | Disadvantage block 2 Higher Funding                                                          |
		| Calculation | 01b96a4a-688e-4396-8904-f882f09fbb62 | 169        | Minimum Funding Top up                                                                       |
		| Calculation | c5a3b0a2-fc1e-4a7c-8be8-fea57cb5d71d | 379        | Total large programme students                                                               |
		| Calculation | da7ec136-2fb9-4778-9e40-78de3fe2c73f | 239        | Programme funding without area cost allowance applied                                        |
		| Calculation | 65c5fbf0-5699-4d9f-a303-1fc55e41df24 | 240        | Area cost factor                                                                             |
		| Calculation | d398f89d-be0c-4834-8b90-5811e207a558 | 238        | Programme funding with area cost allowance applied                                           |
		| Calculation | 4a7b9179-b264-4495-867e-5de3fd5bbed7 | 237        | Area cost allowance                                                                          |
		| Calculation | 27b4bbf3-7aef-4e62-bc55-8d9c954cfc65 | 216        | Band 5 Students not meeting condition of funding                                             |
		| Calculation | 15d349b5-584c-4bfe-9650-42e9476a0175 | 217        | Band 4 Students not meeting condition of funding                                             |
		| Calculation | 56a9b23b-87ba-4853-91d7-b01c95637fdd | 218        | Band 3 Students not meeting condition of funding                                             |
		| Calculation | 3068f1c5-15fc-46ca-b688-61f8c7910551 | 219        | Band 2 Students not meeting condition of funding                                             |
		| Calculation | d574bee3-d494-4733-b2f4-febcaa69dbe2 | 220        | Band 1 Students not meeting condition of funding                                             |
		| Calculation | 76fe5221-e365-4d4e-a96c-7c5fe97c1b86 | 205        | Band 5 National Funding Lagged Rate                                                          |
		| Calculation | b3f4e188-f938-4f69-8385-725928f51021 | 211        | Band 4 National Funding Lagged Rate                                                          |
		| Calculation | 3f30d2c4-abf3-4839-b1db-d97982b2e217 | 212        | Band 3 National Funding Lagged Rate                                                          |
		| Calculation | 74c1002d-e3e9-450e-8e2d-9b951c050765 | 213        | Band 2 National Funding Lagged Rate                                                          |
		| Calculation | e1bf7483-9e52-44e3-aba7-0c72e9e20a5c | 235        | Band 1 FTE not meeting condition of funding                                                  |
		| Calculation | d9889c0e-498a-42af-b89d-01518040f7e9 | 215        | Total Students not meeting Condition of funding                                              |
		| Calculation | 734604c6-c68c-4b16-8d12-4f1071f79ecb | 221        | Band 5 Funding for CoF Non Compliant students                                                |
		| Calculation | c4eca2d2-0596-4e37-839a-2315773f3514 | 222        | Band 4 Funding for CoF Non Compliant students                                                |
		| Calculation | 70f45ad0-7119-4d02-9acd-238779df6735 | 223        | Band 3 Funding for CoF Non Compliant students                                                |
		| Calculation | c4bcbc74-1750-4654-888e-9bff582bb06b | 224        | Band 2 Funding for CoF Non Compliant students                                                |
		| Calculation | 7b2c84a6-62f3-4f82-a1d1-b235b24b6e94 | 225        | Band 1 FTE Funding for CoF non compliant students                                            |
		| Calculation | 964f3e3b-f89c-4510-8856-72c63986a41e | 199        | Band 5 Students excluding 19+ Full year data                                                 |
		| Calculation | 5804bc28-4301-41b0-a622-759f2c6be887 | 200        | Band 4 Students excluding 19+ full year data                                                 |
		| Calculation | 47fbb66a-ab86-4edf-a109-280a5ba6aacb | 201        | Band 3 Students excluding 19+ full year data                                                 |
		| Calculation | 2886704a-8534-4740-802d-831498acfc5e | 202        | Band 2 Students excluding 19+ full year data                                                 |
		| Calculation | a159261d-cc3b-49d8-9609-da5eb164a9bf | 203        | Band 1 Students excluding 19+ full year data                                                 |
		| Calculation | 2b8aae02-5d4c-49fa-8158-6f069c395e65 | 434        | Data Source                                                                                  |
		| Calculation | 857d0cbd-1ba4-4593-9a93-1b3ad58f97d4 | 214        | Band 1 FTE excluding 19+ full year data                                                      |
		| Calculation | 0e81d9bf-1da8-40f9-b8fb-73e1ed6e6c0a | 198        | Total Students Excluding 19+ Full year data                                                  |
		| Calculation | df2d07f9-c90c-4699-8f7a-7fc07d0644e2 | 204        | Band 5 Funding Full Year Students excluding 19+                                              |
		| Calculation | 5806c9dd-7066-4fcc-adc0-da062dc2ef4e | 207        | Band 4 Funding Full Year Students excluding 19+                                              |
		| Calculation | 091cba13-2258-48b2-a85b-4ae55be28c29 | 208        | Band 3 Funding Full Year Students excluding 19+                                              |
		| Calculation | 58c66937-8093-4168-bf4f-eccb2e3aacbf | 209        | Band 2 Funding Full Year Students excluding 19+                                              |
		| Calculation | d544c44e-b6b5-431e-ae4a-5c8ed92dae65 | 210        | Band 1 FTE Funding Full Year Students excluding 19+                                          |
		| Calculation | bccbb3b8-0abd-4194-b659-734803ec7fe4 | 196        | Total Funding Full Year excluding 19+                                                        |
		| Calculation | dad22e0a-1025-45b8-980a-44eb30700e88 | 197        | Condition of funding tolerance                                                               |
		| Calculation | 7995d48f-935f-4190-bf37-8102a0538f2e | 192        | Condition of funding adjustment above tolerance                                              |
		| Calculation | f73b383d-9147-4cb8-a277-49a1e90820cf | 193        | Condition of funding Reduced Rate                                                            |
		| Calculation | fff084e0-24ee-47e6-9b3c-eddca35f2e24 | 194        | Total funding for CoF non compliant students                                                 |
		| Calculation | c9f6dafe-d65f-4bc3-b468-af96a07723ae | 195        | Tolerance applied to Total Funding Full Year Students excluding 19+                          |
		| Calculation | 2fa7ba90-686a-4f46-9078-e913e1eb7e7a | 191        | Condition of funding adjustment                                                              |
		| Calculation | d15867ca-0e79-457d-a964-d1f935e3ab7c | 424        | Offset high value courses for school and college leavers in year programme funding           |
		| Calculation | 14a82c4d-802d-4f67-926f-81de92cab917 | 255        | Financial Disadvantage instances per student                                                 |
		| Calculation | ea2b0af5-85f1-4b3b-9a5c-93df58f847ab | 437        | Funded Students Lagged Or High Needs                                                         |
		| Calculation | 28d1745f-f977-4a3b-8e96-6a97d782c78c | 252        | Financial Disadvantage Number of instances                                                   |
		| Calculation | e3f049d6-22fa-435e-88e9-093fecc5eca1 | 253        | Financial Disadvantage instance Rate                                                         |
		| Calculation | 39378dbe-c5e5-4882-9a46-a4f8606c5d8e | 251        | Financial Disadvantage Funding                                                               |
		| Calculation | 12a78abb-070b-45fe-8432-ea38d4d09232 | 260        | Student Costs Travel Instances per student                                                   |
		| Calculation | e0b79abc-5609-4236-a049-d5d28b549d37 | 257        | Student Costs Travel Number of instances                                                     |
		| Calculation | d7265454-922e-4dc5-83f9-d82983bf76be | 258        | Student Costs Travel Funding instance Rate                                                   |
		| Calculation | 2587e2ec-0280-4e3f-9161-fb5e62932468 | 256        | Student Costs Travel Funding                                                                 |
		| Calculation | cf051bc2-16e2-465d-a71f-a49118209762 | 262        | Student Costs Industry placement Number of instances                                         |
		| Calculation | 5070f082-8f7a-4648-aad3-dd31387acbed | 264        | Student Costs Industry Placements rate                                                       |
		| Calculation | f1148e18-d82b-4ef6-9320-b29104a89e4d | 261        | Student Costs Industry Placements funding                                                    |
		| Calculation | 3d1dbda3-ee02-45bd-8dc5-d76b1fa666cc | 250        | Bursary adjustment in respect of Free meals                                                  |
		| Calculation | 6727fea3-d6ca-4e2d-8618-b209a919eeda | 245        | Exceptional adjustment to Discretionary Bursary Fund                                         |
		| Calculation | b2fd937f-1367-420a-a25d-2379fd71a410 | 265        | Transition Lower Limit                                                                       |
		| Calculation | df678971-68b4-4c6b-88e4-362718c71a42 | 266        | Transition upper limit                                                                       |
		| Calculation | 4a4745b7-4241-4b63-99ee-4f0c0482d6fd | 267        | Baseline for Discretionary bursary Transition fixed 2019 to 2020                             |
		| Calculation | 32e5bf02-1e9f-4b98-a06a-e4f49c531d23 | 409        | Discretionary bursary transition adjustment                                                  |
		| Calculation | d851dc56-9e36-4afc-8551-179055e1995c | 269        | Residential Bursary Fund                                                                     |
		| Calculation | 93d68f09-a0c2-42fe-baf9-0276433e8de7 | 270        | Residential Support Scheme                                                                   |
		| Calculation | 94a3b3e4-c24a-438e-a6c4-23935da28bf3 | 290        | Free Meals Students                                                                          |
		| Calculation | 626cc08a-25a0-4f4f-a373-6b97afbf419c | 363        | Total Students full year data for use in free meals                                          |
		| Calculation | 5e5d1249-e8da-44e7-8aa7-a96024e93c2d | 289        | Proportion of students on Free Meals                                                         |
		| Calculation | 95e94909-df15-446e-bbb5-2a796353d29b | 286        | Total Students Funded for Free Meals for Current Year                                        |
		| Calculation | 01fb7c9d-ec74-4f43-b404-c918f7fe7fe5 | 282        | Free Meals Students attracting the Higher Rate                                               |
		| Calculation | 5419db0b-d463-4c97-a0db-2c2f679a0e24 | 283        | Free Meals Higher Rate                                                                       |
		| Calculation | be43e79d-e36d-49f9-8fd8-8f2c7bc3d0bd | 281        | Free meals Higher Funding                                                                    |
		| Calculation | ecf19c2a-6373-429c-a8de-2f4a7322e84f | 292        | Free Meals Students attracting the Lower Rate                                                |
		| Calculation | 881ea2c3-997e-463a-b316-398bd9d833ba | 293        | Free Meals Lower Rate                                                                        |
		| Calculation | a1f45664-cd06-4572-a580-f5afd1996b89 | 291        | Free Meals Lower Funding                                                                     |
		| Calculation | 2d5d37f5-faea-48b4-b033-a60a2544b031 | 296        | Free Meals Students attracting the FTE Rate                                                  |
		| Calculation | d7c65add-6884-45c8-8353-c3857f98b3cf | 295        | Free Meals FTE Funding                                                                       |
		| Calculation | 34836ed7-3953-4c3f-8f56-1d410c716fdf | 275        | Free meals administration cost                                                               |
		| Calculation | 18a9f732-944a-4d23-b713-43be830ea9b6 | 298        | Free Meals Exceptional Adjustment                                                            |
		| Calculation | 0b6a65dc-8449-4643-bf30-07690dbeee1c | 354        | Disadvantage Accommodation Costs                                                             |
		| Calculation | 02b55a1e-685d-4524-a0ff-097a795932c5 | 321        | Industry placements T-Level Rate                                                             |
		| Calculation | 99ca3111-a417-4bbf-aa9e-006c697375bf | 319        | Industry Placements T-Level Funding                                                          |
		| Calculation | ca034370-fb4c-4073-8b05-97fe7b5f0857 | 401        | Industry Placements Capacity and Delivery Qualifying Students                                |
		| Calculation | 648948e3-58e6-4592-8424-adeb24fc0581 | 402        | Industry Placements Capacity and Delivery rate                                               |
		| Calculation | d20f9bed-4032-491c-9300-aeb41915dcf1 | 399        | Industry Placements Capacity and Delivery Funding                                            |
		| Calculation | 1ead4132-fbb5-4bc9-8506-33b08e10e587 | 316        | Alternative Completions - Sporting Excellence                                                |
		| Calculation | 5b59fdd2-60ab-4127-822b-fa81c3962f8d | 414        | Alternative Completions - Sea Fishing                                                        |
		| Calculation | f99102a3-5024-48db-b9e4-db1fc9a69043 | 392        | Post Opening Grant - Per Pupil Resources                                                     |
		| Calculation | 7ba82116-0fcc-4e7d-9f88-ae21744d2651 | 393        | Post Opening Grant - Leadership Diseconomies                                                 |
		| Calculation | 62156712-8463-4630-a76f-13b461711ab9 | 421        | Start Up Grant Part A                                                                        |
		| Calculation | 7d68e00b-b7c2-477f-b4fe-5b8cff4e229a | 422        | Start Up Grant Part B                                                                        |
		| Calculation | deb8cc38-c170-4567-8f14-20b5dcad8922 | 380        | Current year Total Programme funding per student - SPI                                       |
		| Calculation | 90d9f82d-0524-4db8-9cfa-f14cabceda02 | 330        | 16-19 High needs students                                                                    |
		| Calculation | b7b2ff6f-a347-4853-b051-fc128913776c | 331        | 19-24 High Needs students                                                                    |
		| Calculation | 3f437067-7ef1-481c-a631-db5d33031241 | 387        | R06 16-19 High needs students                                                                |
		| Calculation | 5d9983ac-8748-4780-95d9-16f877c299da | 388        | R06 19-24 High needs students                                                                |
		| Calculation | 9c6540c4-5d9a-4655-a24d-560ead889e68 | 334        | Total R06 High Needs Student Number                                                          |
		| Calculation | 45550274-958a-4042-be9d-775a996946d4 | 328        | Total High Needs Students                                                                    |
		| Calculation | 03d0227b-7a19-456f-b0fa-23048026e96a | 329        | High Needs Rate                                                                              |
		| Calculation | 17df4975-f739-4574-8f06-6aafd3a28ad8 | 332        | 16-19 High Needs Student Proportion                                                          |
		| Calculation | 0fe3fa89-8361-46d9-b584-e415dfc5675a | 335        | 19-24 High Needs Student proportion                                                          |
		| Calculation | fadc2ef0-417c-4cde-a8d7-04cad69647b1 | 383        | Exceptional variations to High Needs student number                                          |
		| Calculation | de1bd71b-29e0-4f48-b4d2-71384e92b9f5 | 327        | High Needs Element 2 Student Funding                                                         |
		| Calculation | 8159aafd-095a-4e3d-a580-d005a5ad52d4 | 314        | Advanced Maths Baseline Students                                                             |
		| Calculation | 7ad71f61-14aa-49b5-8101-0885d1767f42 | 315        | Advanced Maths Eligible Students                                                             |
		| Calculation | ffc2ed9a-98ee-44e0-af6f-b782b9bcd76a | 312        | Advanced Maths Rate                                                                          |
		| Calculation | 8953b6c3-6fed-4a27-b2ed-9c854b5f5a90 | 313        | Advanced Maths Eligible Students minus baseline students                                     |
		| Calculation | 518951bb-0002-4ddc-80ce-8ee039398d91 | 311        | Advanced Maths Premium Funding                                                               |
		| Calculation | e32aeb34-427f-404c-9867-43a178d991c3 | 307        | Number of qualifying High Value Course Students                                              |
		| Calculation | d34132de-0abf-4126-b552-9d65024a450f | 308        | High Value Course Premium Rate                                                               |
		| Calculation | 3bbfa0fc-0380-4903-b57d-804176d9f65e | 306        | High Value Course Premium Funding                                                            |
		| Calculation | 61fd3afc-d74a-450b-a6c3-0858260112df | 340        | Teachers Pension Annual Payments for previous full financial year                            |
		| Calculation | 91f7a24a-67e7-4743-a727-28ada5333bc2 | 347        | Employer contribution rate included                                                          |
		| Calculation | 0aa77eef-37cc-4879-809b-5f3a204af156 | 348        | Employer contribution rate previous FY                                                       |
		| Calculation | 91841fdd-7f62-4f19-bc07-f72f45c53ae2 | 349        | OBR Wage growth forecast previous Year                                                       |
		| Calculation | 25d5b084-4274-4edb-ae49-699197eb6eee | 343        | Teachers Pension Annual payments including Employer contributions increase for previous year |
		| Calculation | c1575687-5bb1-4604-a524-dcbed3a3e3f0 | 351        | Teachers Pension annual pay with increase with OBR previous year applied                     |
		| Calculation | b99d7944-1874-497b-90ff-5590039a7d96 | 352        | OBR Wage Growth Forecast current year                                                        |
		| Calculation | 5a628ba1-94ac-440e-b78d-296f5f2b5da7 | 344        | Teachers Pension uplift for previous FY OBR wage growth                                      |
		| Calculation | c8204022-f046-4606-ad8d-d9683bfb25a6 | 345        | Teacher Pension uplift for current FY OBR wage growth                                        |
		| Calculation | d89bf663-2ed8-44d2-935c-b0cf9a9a84f2 | 341        | Teachers Pension Revised Annual Cost                                                         |
		| Calculation | 83a4a011-ac69-4213-95e3-395aa6a6bfe0 | 339        | Difference between Teachers Pension available FY payments and Revised Annual Cost            |
		| Calculation | 8643d35e-e326-4628-9e52-96908c53e2d9 | 395        | Maths Top Up                                                                                 |
		| Calculation | b09f67a1-343f-4359-ae43-7e324858381d | 430        | High value courses for school and college leavers eligible students                          |
		| Calculation | e87eae67-5495-451e-a531-9abebd2f23e0 | 431        | High value courses for school and college leavers baseline                                   |
		| Calculation | ac4d0e5d-1afe-410f-a65b-4913ba85ce92 | 427        | High value courses for school and college leavers students above baseline                    |
		| Calculation | 8e0335fd-2d7b-44ea-9c20-ffa7803cf75b | 428        | High value courses for school and college leavers rate                                       |
		| Calculation | 6d5a0965-470e-4794-a0ac-4e1d80a18736 | 435        | High value courses for school and college leavers previously funded                          |
		| Calculation | e5105ca6-3ef2-443e-830a-b915cb8a42dd | 436        | High value courses for school and college leavers additional students                        |
		| Calculation | c6ab4f61-cd95-4596-ac74-47ce66ff997b | 426        | High value courses for school and college leavers uplift funding                             |
		| Calculation | bb72366d-e7ab-40f9-9fc6-f62ec41c9cc9 | 440        | LA Maintained Special School Bursary Funding                                                 |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value        |
		| 169                   | 1437.5       |
		| 321                   | 275          |
		| 376                   | 480          |
		| 27                    | 117.6237624  |
		| 329                   | 6000         |
		| 147                   | 480          |
		| 348                   | 23.6         |
		| 106                   | 4363         |
		| 198                   | 101          |
		| 111                   | -4006.545735 |
		| 260                   | 0.27558      |
		| 36                    | 101          |
		| 44                    | Census S02   |
		| 256                   | 15476.5728   |
		| 434                   | Census       |
		| 166                   | 650          |
		| 35                    | 99           |
		| 69                    | 0.00990099   |
		| 107                   | 120          |
		| 306                   | 11200        |
		| 212                   | 2700         |
		| 113                   | 500818.2168  |
		| 85                    | 2234         |
		| 267                   | 15046.02     |
		| 237                   | 5230.7351    |
		| 264                   | 48           |
		| 311                   | 11400        |
		| 55                    | 120          |
		| 156                   | 9.5052       |
		| 283                   | 358          |
		| 132                   | 750          |
		| 200                   | 2            |
		| 207                   | 6600         |
		| 103                   | 5061         |
		| 129                   | 120          |
		| 131                   | 4.752        |
		| 402                   | 210          |
		| 240                   | 1.01         |
		| 39                    | 1            |
		| 99                    | 5584         |
		| 213                   | 2133         |
		| 372                   | 817          |
		| 205                   | 4000         |
		| 72                    | 0.00990099   |
		| 315                   | 51           |
		| 82                    | 2827         |
		| 314                   | 32           |
		| 158                   | 0.07921      |
		| 177                   | 838          |
		| 293                   | 179          |
		| 313                   | 19           |
		| 28                    | 4188         |
		| 428                   | 400          |
		| 130                   | 3564         |
		| 349                   | 0.012        |
		| 347                   | 16.4         |
		| 133                   | 0.0396       |
		| 199                   | 99           |
		| 68                    | 3455         |
		| 182                   | 419          |
		| 77                    | 0.01980198   |
		| 116                   | 0.992        |
		| 117                   | 16697.84023  |
		| 386                   | 120          |
		| 432                   | 8209.90099   |
		| 26                    | 492608.3168  |
		| 40                    | 1            |
		| 126                   | 375          |
		| 178                   | 2            |
		| 374                   | 4562.496     |
		| 265                   | 7523.01      |
		| 211                   | 3300         |
		| 141                   | 517073.5113  |
		| 121                   | 1.03361      |
		| 352                   | 0.021        |
		| 155                   | 292          |
		| 196                   | 402600       |
		| 73                    | 2.376237624  |
		| 195                   | 20130        |
		| 30                    | 117.6237624  |
		| 308                   | 400          |
		| 238                   | 528304.2451  |
		| 47                    | 120          |
		| 258                   | 468          |
		| 114                   | 496811.6711  |
		| 143                   | 1            |
		| 239                   | 517073.51    |
		| 197                   | 0.05         |
		| 96                    | 6108         |
		| 204                   | 396000       |
		| 266                   | 22569.03     |
		| 257                   | 33.0696      |
		| 307                   | 28           |
		| 253                   | 243          |
		| 437                   | 120          |
		| 375                   | 9.5052       |
		| 34                    | 0.98019802   |
		| 193                   | 0.5          |
		| 38                    | 2            |
		| 33                    | 120          |
		| 118                   | 513509.5113  |
		| 312                   | 600          |
		| 11                    |              |
		| 9                     |              |
		| 10                    |              |
		| 176                   |              |
		| 175                   |              |
		| 181                   |              |
		| 180                   |              |
		| 53                    |              |
		| 49                    |              |
		| 51                    |              |
		| 48                    |              |
		| 433                   |              |
		| 45                    |              |
		| 58                    |              |
		| 57                    |              |
		| 54                    |              |
		| 46                    |              |
		| 385                   |              |
		| 418                   |              |
		| 384                   |              |
		| 59                    |              |
		| 60                    |              |
		| 56                    |              |
		| 41                    |              |
		| 42                    |              |
		| 43                    |              |
		| 95                    |              |
		| 98                    |              |
		| 102                   |              |
		| 105                   |              |
		| 25                    |              |
		| 81                    |              |
		| 80                    |              |
		| 78                    |              |
		| 86                    |              |
		| 84                    |              |
		| 83                    |              |
		| 90                    |              |
		| 88                    |              |
		| 93                    |              |
		| 87                    |              |
		| 92                    |              |
		| 94                    |              |
		| 97                    |              |
		| 101                   |              |
		| 104                   |              |
		| 127                   |              |
		| 125                   |              |
		| 124                   |              |
		| 139                   |              |
		| 146                   |              |
		| 145                   |              |
		| 154                   |              |
		| 153                   |              |
		| 164                   |              |
		| 161                   |              |
		| 162                   |              |
		| 167                   |              |
		| 165                   |              |
		| 379                   |              |
		| 216                   |              |
		| 217                   |              |
		| 218                   |              |
		| 219                   |              |
		| 220                   |              |
		| 235                   |              |
		| 215                   |              |
		| 221                   |              |
		| 222                   |              |
		| 223                   |              |
		| 224                   |              |
		| 225                   |              |
		| 201                   |              |
		| 202                   |              |
		| 203                   |              |
		| 214                   |              |
		| 208                   |              |
		| 209                   |              |
		| 210                   |              |
		| 192                   |              |
		| 194                   |              |
		| 191                   |              |
		| 424                   |              |
		| 255                   |              |
		| 252                   |              |
		| 251                   |              |
		| 262                   |              |
		| 261                   |              |
		| 250                   |              |
		| 245                   |              |
		| 409                   |              |
		| 269                   |              |
		| 270                   |              |
		| 290                   |              |
		| 363                   |              |
		| 289                   |              |
		| 286                   |              |
		| 282                   |              |
		| 281                   |              |
		| 292                   |              |
		| 291                   |              |
		| 296                   |              |
		| 295                   |              |
		| 275                   |              |
		| 298                   |              |
		| 354                   |              |
		| 319                   |              |
		| 401                   |              |
		| 399                   |              |
		| 316                   |              |
		| 414                   |              |
		| 392                   |              |
		| 393                   |              |
		| 421                   |              |
		| 422                   |              |
		| 380                   |              |
		| 330                   |              |
		| 331                   |              |
		| 387                   |              |
		| 388                   |              |
		| 334                   |              |
		| 328                   |              |
		| 332                   |              |
		| 335                   |              |
		| 383                   |              |
		| 327                   |              |
		| 340                   |              |
		| 343                   |              |
		| 351                   |              |
		| 344                   |              |
		| 345                   |              |
		| 341                   |              |
		| 339                   |              |
		| 395                   |              |
		| 430                   |              |
		| 431                   |              |
		| 427                   |              |
		| 435                   |              |
		| 436                   |              |
		| 426                   |              |
		| 440                   |              |
	And the Published Provider has the following provider information
		| Field                         | Value                         |
		| ProviderId                    | 1000000                       |
		| Name                          | Academy 1                     |
		| Authority                     | Local Authority 1             |
		| DateOpened                    | 2012-03-15                    |
		| ProviderVersionId             | <ProviderVersionId>           |
		| TrustStatus                   | Not Supported By A Trust      |
		| UKPRN                         | 1000000                       |
		| TrustStatus                   | SupportedByAMultiAcademyTrust |
		| Status                        | Open                          |
		| ProviderType                  | Acade                         |
		| ProviderSubType               | 11ACA                         |
		| PaymentOrganisationIdentifier | 9000000                       |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                         |
		| ProviderId                    | 1000000                       |
		| Name                          | Academy 1                     |
		| Authority                     | Local Authority 1             |
		| DateOpened                    | 2012-03-15                    |
		| ProviderVersionId             | <ProviderVersionId>           |
		| TrustStatus                   | Not Supported By A Trust      |
		| UKPRN                         | 1000000                       |
		| TrustStatus                   | SupportedByAMultiAcademyTrust |
		| Status                        | Open                          |
		| ProviderType                  | Acade                         |
		| ProviderSubType               | 11ACA                         |
		| PaymentOrganisationIdentifier | 9000000                       |
	And the provider with id '1000000' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000002           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Draft             |
		| TotalFunding    | 566380.82         |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name                                                                                 | FundingLineCode | Value     | TemplateLineId | Type        |
		| Total Funding Allocation                                                             |                 | 566380.82 | 0              | Information |
		| Programme Funding                                                                    | 1619-001        | 528304.24 | 1              | Payment     |
		| Retention Factor Funding                                                             |                 | -4006.55  | 108            | Information |
		| Programme Cost Weighting Funding                                                     |                 | 16697.84  | 109            | Information |
		| Level 3 Maths And English Funding                                                    |                 | 3564      | 110            | Information |
		| Student Funding                                                                      |                 | 500818.22 | 12             | Information |
		| Level 3 Maths And English One Year Funding                                           |                 | 0         | 122            | Information |
		| Level 3 Maths And English Two Year Funding                                           |                 | 3564      | 123            | Information |
		| Total Mainstream Bands Funding                                                       |                 | 500818.22 | 13             | Information |
		| Disadvantage Funding                                                                 |                 | 6000      | 134            | Information |
		| Disadvantage Block 1 And Block 2 Total                                               |                 | 4562.5    | 136            | Information |
		| Disadvantage Block 1 Total Funding                                                   |                 | 0         | 137            | Information |
		| Economic Deprivation Funding                                                         |                 | 0         | 138            | Information |
		| Total T Level Bands Funding                                                          |                 | 0         | 14             | Information |
		| Care Leaver Funding                                                                  |                 | 0         | 144            | Information |
		| Disadvantage Block 2 Total Funding                                                   |                 | 4562.5    | 148            | Information |
		| Disadvantage Block 2 Lower Funding                                                   |                 | 0         | 149            | Information |
		| Band 5 Student Funding                                                               |                 | 492608.32 | 15             | Information |
		| Disadvantage Block 2 FTE Funding                                                     |                 | 0         | 150            | Information |
		| Disadvantage Block 2 T Level Funding                                                 |                 | 0         | 151            | Information |
		| Band 4 Student Funding                                                               |                 | 8209.9    | 16             | Information |
		| Minimum Top Up Funding                                                               |                 | 1437.5    | 168            | Information |
		| Band 3 Student Funding                                                               |                 | 0         | 17             | Information |
		| Total Large Programme Funding                                                        |                 | 0         | 173            | Information |
		| Large Programme Funding Uplift At 20 Percent National Rate                           |                 | 0         | 174            | Information |
		| Large Programme Funding Uplift At 10 Percent National Rate                           |                 | 0         | 179            | Information |
		| Band 2 Student Funding                                                               |                 | 0         | 18             | Information |
		| Programme Funding Without Area Cost Applied                                          |                 | 523073.51 | 184            | Information |
		| Band 1 FTE Student Funding                                                           |                 | 0         | 19             | Information |
		| Total Care Standards Funding                                                         |                 | 0         | 2              | Information |
		| T Level Band 9 Funding                                                               |                 | 0         | 21             | Information |
		| T Level Band 8 Funding                                                               |                 | 0         | 22             | Information |
		| T Level Band 7 Funding                                                               |                 | 0         | 23             | Information |
		| Area Cost Allowance Adjustment                                                       |                 | 5230.74   | 236            | Information |
		| T Level B and 6 Funding                                                              |                 | 0         | 24             | Information |
		| Student Financial Support Funding                                                    |                 | 15476.57  | 241            | Information |
		| Discretionary Bursary Fund Total                                                     | 1619-002        | 15476.57  | 242            | Payment     |
		| Discretionary Bursary Fund                                                           |                 | 15476.57  | 243            | Information |
		| Exceptional Adjustment To Discretionary Bursary Fund                                 |                 | 0         | 244            | Information |
		| Financial Disadvantage Funding                                                       |                 | 0         | 246            | Information |
		| Student Costs Travel Funding                                                         |                 | 15476.57  | 247            | Information |
		| Student Costs Industry Placement Funding                                             |                 | 0         | 248            | Information |
		| Bursary Adjustment In Respect Of FreeMeals                                           |                 | 0         | 249            | Information |
		| Residential Funding Total                                                            |                 | 0         | 268            | Information |
		| Total Free Meals Funding                                                             | 1619-005        | 0         | 271            | Payment     |
		| Free Meals Administration                                                            |                 | 0         | 272            | Information |
		| Free Meals Total Including Exception                                                 |                 | 0         | 276            | Information |
		| Free Meals Higher Funding                                                            |                 | 0         | 277            | Information |
		| Free Meals Lower Funding                                                             |                 | 0         | 278            | Information |
		| Free Meals FTE Funding                                                               |                 | 0         | 279            | Information |
		| Free Meals Exceptional Adjustment                                                    |                 | 0         | 287            | Information |
		| High Needs Element 2 Student Funding                                                 | 1619-006        | 0         | 299            | Payment     |
		| Industry Placements Funding                                                          | 1619-007        | 0         | 300            | Payment     |
		| Advanced Maths Premium Funding                                                       | 1619-008        | 11400     | 301            | Payment     |
		| High Value Courses Premium Funding                                                   | 1619-009        | 11200     | 302            | Payment     |
		| Teachers Pension Scheme Grant                                                        | 1619-010        | 0         | 303            | Payment     |
		| Alternative Completions Funding                                                      |                 | 0         | 304            | Information |
		| Residential Bursary Fund                                                             | 1619-003        | 0         | 309            | Payment     |
		| Residential Support Scheme                                                           | 1619-004        | 0         | 310            | Payment     |
		| Industry Placements T Level Funding                                                  |                 | 0         | 318            | Information |
		| Disadvantage Accommodation Costs                                                     | 1619-012        | 0         | 353            | Payment     |
		| Disadvantage Block 2 Higher Funding                                                  |                 | 4562.5    | 373            | Information |
		| Cummulative Programme Funding Without Total Large Programme And Disadvantage Funding |                 | 517073.51 | 381            | Information |
		| Cummulative Programme Funding With Disadvantage Funding                              |                 | 523073.51 | 382            | Information |
		| Total Start Up And Post Opening Grant                                                |                 | 0         | 389            | Information |
		| Post Opening Grant Per Pupil Resources                                               | 1619-017        | 0         | 390            | Payment     |
		| Post Opening Grant - Leadership Diseconomies                                         | 1619-018        | 0         | 391            | Payment     |
		| Maths Top Up                                                                         | 1619-019        | 0         | 394            | Payment     |
		| Industry Placements Capacity And Delivery Funding                                    |                 | 0         | 396            | Information |
		| Programme Funding Without Care Standards Or Condition Of Funding Adjustment          |                 | 528304.24 | 4              | Information |
		| Discretionary Bursary Transition Adjustment                                          |                 | 0         | 408            | Information |
		| Alternative Completions Sporting Excellence                                          | 1619-011        | 0         | 410            | Payment     |
		| Alternative Completions - Sea Fishing                                                | 1619-020        | 0         | 412            | Payment     |
		| Start Up Grant Part A                                                                | 1619-022        | 0         | 419            | Payment     |
		| Start Up Grant Part B                                                                | 1619-023        | 0         | 420            | Payment     |
		| Offset High Value Courses For School And College Leavers In Year Programme Funding   |                 | 0         | 423            | Information |
		| High Value Courses For School And College Leavers Total                              | 1619-024        | 0         | 425            | Payment     |
		| Total Funding Allocation And LAMSS Bursary Funding                                   |                 | 566380.82 | 438            | Information |
		| LA Maintained Special School Bursary Funding                                         | 1619-013        | 0         | 439            | Payment     |
		| Condition Of Funding Adjustment                                                      |                 | 0         | 5              | Information |
		| Care Standards Student Funding                                                       |                 | 0         | 6              | Information |
		| Care Standards Institution Lump Sum Funding                                          |                 | 0         | 7              | Information |
	And the Published Provider has the following distribution period for funding line '1619-001'
		| DistributionPeriodId | Value     |
		| AS-1920              | 7000      |
		| AS-2021              | 566380.82 |
	And the Published Providers distribution period has the following profiles for funding line '1619-001'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AS-1920              | CalendarMonth | October   | 1920 | 1          | 7000          |
		| AS-2021              | CalendarMonth | April     | 2021 | 1          | 566380.82     |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value        |
		| 169                   | 1437.5       |
		| 321                   | 275          |
		| 376                   | 480          |
		| 27                    | 117.6237624  |
		| 329                   | 6000         |
		| 147                   | 480          |
		| 348                   | 23.6         |
		| 106                   | 4363         |
		| 198                   | 101          |
		| 111                   | -4006.545735 |
		| 260                   | 0.27558      |
		| 36                    | 101          |
		| 44                    | Census S02   |
		| 256                   | 15476.5728   |
		| 434                   | Census       |
		| 166                   | 650          |
		| 35                    | 99           |
		| 69                    | 0.00990099   |
		| 107                   | 120          |
		| 306                   | 11200        |
		| 212                   | 2700         |
		| 113                   | 500818.2168  |
		| 85                    | 2234         |
		| 267                   | 15046.02     |
		| 237                   | 5230.7351    |
		| 264                   | 48           |
		| 311                   | 11400        |
		| 55                    | 120          |
		| 156                   | 9.5052       |
		| 283                   | 358          |
		| 132                   | 750          |
		| 200                   | 2            |
		| 207                   | 6600         |
		| 103                   | 5061         |
		| 129                   | 120          |
		| 131                   | 4.752        |
		| 402                   | 210          |
		| 240                   | 1.01         |
		| 39                    | 1            |
		| 99                    | 5584         |
		| 213                   | 2133         |
		| 372                   | 817          |
		| 205                   | 4000         |
		| 72                    | 0.00990099   |
		| 315                   | 51           |
		| 82                    | 2827         |
		| 314                   | 32           |
		| 158                   | 0.07921      |
		| 177                   | 838          |
		| 293                   | 179          |
		| 313                   | 19           |
		| 28                    | 4188         |
		| 428                   | 400          |
		| 130                   | 3564         |
		| 349                   | 0.012        |
		| 347                   | 16.4         |
		| 133                   | 0.0396       |
		| 199                   | 99           |
		| 68                    | 3455         |
		| 182                   | 419          |
		| 77                    | 0.01980198   |
		| 116                   | 0.992        |
		| 117                   | 16697.84023  |
		| 386                   | 120          |
		| 432                   | 8209.90099   |
		| 26                    | 492608.3168  |
		| 40                    | 1            |
		| 126                   | 375          |
		| 178                   | 2            |
		| 374                   | 4562.496     |
		| 265                   | 7523.01      |
		| 211                   | 3300         |
		| 141                   | 517073.5113  |
		| 121                   | 1.03361      |
		| 352                   | 0.021        |
		| 155                   | 292          |
		| 196                   | 402600       |
		| 73                    | 2.376237624  |
		| 195                   | 20130        |
		| 30                    | 117.6237624  |
		| 308                   | 400          |
		| 238                   | 528304.2451  |
		| 47                    | 120          |
		| 258                   | 468          |
		| 114                   | 496811.6711  |
		| 143                   | 1            |
		| 239                   | 517073.51    |
		| 197                   | 0.05         |
		| 96                    | 6108         |
		| 204                   | 396000       |
		| 266                   | 22569.03     |
		| 257                   | 33.0696      |
		| 307                   | 28           |
		| 253                   | 243          |
		| 437                   | 120          |
		| 375                   | 9.5052       |
		| 34                    | 0.98019802   |
		| 193                   | 0.5          |
		| 38                    | 2            |
		| 33                    | 120          |
		| 118                   | 513509.5113  |
		| 312                   | 600          |
		| 11                    |              |
		| 9                     |              |
		| 10                    |              |
		| 176                   |              |
		| 175                   |              |
		| 181                   |              |
		| 180                   |              |
		| 53                    |              |
		| 49                    |              |
		| 51                    |              |
		| 48                    |              |
		| 433                   |              |
		| 45                    |              |
		| 58                    |              |
		| 57                    |              |
		| 54                    |              |
		| 46                    |              |
		| 385                   |              |
		| 418                   |              |
		| 384                   |              |
		| 59                    |              |
		| 60                    |              |
		| 56                    |              |
		| 41                    |              |
		| 42                    |              |
		| 43                    |              |
		| 95                    |              |
		| 98                    |              |
		| 102                   |              |
		| 105                   |              |
		| 25                    |              |
		| 81                    |              |
		| 80                    |              |
		| 78                    |              |
		| 86                    |              |
		| 84                    |              |
		| 83                    |              |
		| 90                    |              |
		| 88                    |              |
		| 93                    |              |
		| 87                    |              |
		| 92                    |              |
		| 94                    |              |
		| 97                    |              |
		| 101                   |              |
		| 104                   |              |
		| 127                   |              |
		| 125                   |              |
		| 124                   |              |
		| 139                   |              |
		| 146                   |              |
		| 145                   |              |
		| 154                   |              |
		| 153                   |              |
		| 164                   |              |
		| 161                   |              |
		| 162                   |              |
		| 167                   |              |
		| 165                   |              |
		| 379                   |              |
		| 216                   |              |
		| 217                   |              |
		| 218                   |              |
		| 219                   |              |
		| 220                   |              |
		| 235                   |              |
		| 215                   |              |
		| 221                   |              |
		| 222                   |              |
		| 223                   |              |
		| 224                   |              |
		| 225                   |              |
		| 201                   |              |
		| 202                   |              |
		| 203                   |              |
		| 214                   |              |
		| 208                   |              |
		| 209                   |              |
		| 210                   |              |
		| 192                   |              |
		| 194                   |              |
		| 191                   |              |
		| 424                   |              |
		| 255                   |              |
		| 252                   |              |
		| 251                   |              |
		| 262                   |              |
		| 261                   |              |
		| 250                   |              |
		| 245                   |              |
		| 409                   |              |
		| 269                   |              |
		| 270                   |              |
		| 290                   |              |
		| 363                   |              |
		| 289                   |              |
		| 286                   |              |
		| 282                   |              |
		| 281                   |              |
		| 292                   |              |
		| 291                   |              |
		| 296                   |              |
		| 295                   |              |
		| 275                   |              |
		| 298                   |              |
		| 354                   |              |
		| 319                   |              |
		| 401                   |              |
		| 399                   |              |
		| 316                   |              |
		| 414                   |              |
		| 392                   |              |
		| 393                   |              |
		| 421                   |              |
		| 422                   |              |
		| 380                   |              |
		| 330                   |              |
		| 331                   |              |
		| 387                   |              |
		| 388                   |              |
		| 334                   |              |
		| 328                   |              |
		| 332                   |              |
		| 335                   |              |
		| 383                   |              |
		| 327                   |              |
		| 340                   |              |
		| 343                   |              |
		| 351                   |              |
		| 344                   |              |
		| 345                   |              |
		| 341                   |              |
		| 339                   |              |
		| 395                   |              |
		| 430                   |              |
		| 431                   |              |
		| 427                   |              |
		| 435                   |              |
		| 436                   |              |
		| 426                   |              |
		| 440                   |              |
	And the Published Provider has the following provider information
		| Field                         | Value                         |
		| ProviderId                    | 1000002                       |
		| Name                          | Academy 2                     |
		| Authority                     | Local Authority 1             |
		| DateOpened                    | 2012-03-15                    |
		| LACode                        | 200                           |
		| LocalAuthorityName            | Local Authority 1             |
		| ProviderVersionId             | <ProviderVersionId>           |
		| TrustCode                     | 1001                          |
		| TrustStatus                   | SupportedByAMultiAcademyTrust |
		| UKPRN                         | 1000002                       |
		| Status                        | Open                          |
		| ProviderType                  | Acade                         |
		| ProviderSubType               | FSAP                          |
		| PaymentOrganisationIdentifier | 9000000                       |
	And the Published Provider is available in the repository for this specification
	# Maintained schools in Core Provider Data
	And the following provider exists within core provider data in provider version '<ProviderVersionId>'
		| Field                         | Value                         |
		| ProviderId                    | 1000002                       |
		| Name                          | Academy 2                     |
		| Authority                     | Local Authority 1             |
		| DateOpened                    | 2012-03-15                    |
		| LACode                        | 200                           |
		| LocalAuthorityName            | Local Authority 1             |
		| ProviderVersionId             | <ProviderVersionId>           |
		| TrustCode                     | 1001                          |
		| TrustStatus                   | SupportedByAMultiAcademyTrust |
		| UKPRN                         | 1000002                       |
		| Status                        | Open                          |
		| ProviderType                  | Acade                         |
		| ProviderSubType               | FSAP                          |
		| PaymentOrganisationIdentifier | 9000000                       |
	And the provider with id '1000002' should be a scoped provider in the current specification in provider version '<ProviderVersionId>'
	And the following Published Provider has been previously generated for the current specification
		| Field           | Value             |
		| ProviderId      | 1000003           |
		| FundingStreamId | <FundingStreamId> |
		| FundingPeriodId | <FundingPeriodId> |
		| TemplateVersion | <TemplateVersion> |
		| Status          | Draft             |
		| TotalFunding    | 566380.82         |
		| MajorVersion    | 0                 |
		| MinorVersion    | 1                 |
	And the Published Provider has the following funding lines
		| Name                                                                                 | FundingLineCode | Value     | TemplateLineId | Type        |
		| Total Funding Allocation                                                             |                 | 566380.82 | 0              | Information |
		| Programme Funding                                                                    | 1619-001        | 528304.24 | 1              | Payment     |
		| Retention Factor Funding                                                             |                 | -4006.55  | 108            | Information |
		| Programme Cost Weighting Funding                                                     |                 | 16697.84  | 109            | Information |
		| Level 3 Maths And English Funding                                                    |                 | 3564      | 110            | Information |
		| Student Funding                                                                      |                 | 500818.22 | 12             | Information |
		| Level 3 Maths And English One Year Funding                                           |                 | 0         | 122            | Information |
		| Level 3 Maths And English Two Year Funding                                           |                 | 3564      | 123            | Information |
		| Total Mainstream Bands Funding                                                       |                 | 500818.22 | 13             | Information |
		| Disadvantage Funding                                                                 |                 | 6000      | 134            | Information |
		| Disadvantage Block 1 And Block 2 Total                                               |                 | 4562.5    | 136            | Information |
		| Disadvantage Block 1 Total Funding                                                   |                 | 0         | 137            | Information |
		| Economic Deprivation Funding                                                         |                 | 0         | 138            | Information |
		| Total T Level Bands Funding                                                          |                 | 0         | 14             | Information |
		| Care Leaver Funding                                                                  |                 | 0         | 144            | Information |
		| Disadvantage Block 2 Total Funding                                                   |                 | 4562.5    | 148            | Information |
		| Disadvantage Block 2 Lower Funding                                                   |                 | 0         | 149            | Information |
		| Band 5 Student Funding                                                               |                 | 492608.32 | 15             | Information |
		| Disadvantage Block 2 FTE Funding                                                     |                 | 0         | 150            | Information |
		| Disadvantage Block 2 T Level Funding                                                 |                 | 0         | 151            | Information |
		| Band 4 Student Funding                                                               |                 | 8209.9    | 16             | Information |
		| Minimum Top Up Funding                                                               |                 | 1437.5    | 168            | Information |
		| Band 3 Student Funding                                                               |                 | 0         | 17             | Information |
		| Total Large Programme Funding                                                        |                 | 0         | 173            | Information |
		| Large Programme Funding Uplift At 20 Percent National Rate                           |                 | 0         | 174            | Information |
		| Large Programme Funding Uplift At 10 Percent National Rate                           |                 | 0         | 179            | Information |
		| Band 2 Student Funding                                                               |                 | 0         | 18             | Information |
		| Programme Funding Without Area Cost Applied                                          |                 | 523073.51 | 184            | Information |
		| Band 1 FTE Student Funding                                                           |                 | 0         | 19             | Information |
		| Total Care Standards Funding                                                         |                 | 0         | 2              | Information |
		| T Level Band 9 Funding                                                               |                 | 0         | 21             | Information |
		| T Level Band 8 Funding                                                               |                 | 0         | 22             | Information |
		| T Level Band 7 Funding                                                               |                 | 0         | 23             | Information |
		| Area Cost Allowance Adjustment                                                       |                 | 5230.74   | 236            | Information |
		| T Level B and 6 Funding                                                              |                 | 0         | 24             | Information |
		| Student Financial Support Funding                                                    |                 | 15476.57  | 241            | Information |
		| Discretionary Bursary Fund Total                                                     | 1619-002        | 15476.57  | 242            | Payment     |
		| Discretionary Bursary Fund                                                           |                 | 15476.57  | 243            | Information |
		| Exceptional Adjustment To Discretionary Bursary Fund                                 |                 | 0         | 244            | Information |
		| Financial Disadvantage Funding                                                       |                 | 0         | 246            | Information |
		| Student Costs Travel Funding                                                         |                 | 15476.57  | 247            | Information |
		| Student Costs Industry Placement Funding                                             |                 | 0         | 248            | Information |
		| Bursary Adjustment In Respect Of FreeMeals                                           |                 | 0         | 249            | Information |
		| Residential Funding Total                                                            |                 | 0         | 268            | Information |
		| Total Free Meals Funding                                                             | 1619-005        | 0         | 271            | Payment     |
		| Free Meals Administration                                                            |                 | 0         | 272            | Information |
		| Free Meals Total Including Exception                                                 |                 | 0         | 276            | Information |
		| Free Meals Higher Funding                                                            |                 | 0         | 277            | Information |
		| Free Meals Lower Funding                                                             |                 | 0         | 278            | Information |
		| Free Meals FTE Funding                                                               |                 | 0         | 279            | Information |
		| Free Meals Exceptional Adjustment                                                    |                 | 0         | 287            | Information |
		| High Needs Element 2 Student Funding                                                 | 1619-006        | 0         | 299            | Payment     |
		| Industry Placements Funding                                                          | 1619-007        | 0         | 300            | Payment     |
		| Advanced Maths Premium Funding                                                       | 1619-008        | 11400     | 301            | Payment     |
		| High Value Courses Premium Funding                                                   | 1619-009        | 11200     | 302            | Payment     |
		| Teachers Pension Scheme Grant                                                        | 1619-010        | 0         | 303            | Payment     |
		| Alternative Completions Funding                                                      |                 | 0         | 304            | Information |
		| Residential Bursary Fund                                                             | 1619-003        | 0         | 309            | Payment     |
		| Residential Support Scheme                                                           | 1619-004        | 0         | 310            | Payment     |
		| Industry Placements T Level Funding                                                  |                 | 0         | 318            | Information |
		| Disadvantage Accommodation Costs                                                     | 1619-012        | 0         | 353            | Payment     |
		| Disadvantage Block 2 Higher Funding                                                  |                 | 4562.5    | 373            | Information |
		| Cummulative Programme Funding Without Total Large Programme And Disadvantage Funding |                 | 517073.51 | 381            | Information |
		| Cummulative Programme Funding With Disadvantage Funding                              |                 | 523073.51 | 382            | Information |
		| Total Start Up And Post Opening Grant                                                |                 | 0         | 389            | Information |
		| Post Opening Grant Per Pupil Resources                                               | 1619-017        | 0         | 390            | Payment     |
		| Post Opening Grant - Leadership Diseconomies                                         | 1619-018        | 0         | 391            | Payment     |
		| Maths Top Up                                                                         | 1619-019        | 0         | 394            | Payment     |
		| Industry Placements Capacity And Delivery Funding                                    |                 | 0         | 396            | Information |
		| Programme Funding Without Care Standards Or Condition Of Funding Adjustment          |                 | 528304.24 | 4              | Information |
		| Discretionary Bursary Transition Adjustment                                          |                 | 0         | 408            | Information |
		| Alternative Completions Sporting Excellence                                          | 1619-011        | 0         | 410            | Payment     |
		| Alternative Completions - Sea Fishing                                                | 1619-020        | 0         | 412            | Payment     |
		| Start Up Grant Part A                                                                | 1619-022        | 0         | 419            | Payment     |
		| Start Up Grant Part B                                                                | 1619-023        | 0         | 420            | Payment     |
		| Offset High Value Courses For School And College Leavers In Year Programme Funding   |                 | 0         | 423            | Information |
		| High Value Courses For School And College Leavers Total                              | 1619-024        | 0         | 425            | Payment     |
		| Total Funding Allocation And LAMSS Bursary Funding                                   |                 | 566380.82 | 438            | Information |
		| LA Maintained Special School Bursary Funding                                         | 1619-013        | 0         | 439            | Payment     |
		| Condition Of Funding Adjustment                                                      |                 | 0         | 5              | Information |
		| Care Standards Student Funding                                                       |                 | 0         | 6              | Information |
		| Care Standards Institution Lump Sum Funding                                          |                 | 0         | 7              | Information |
	And the Published Provider has the following distribution period for funding line '1619-001'
		| DistributionPeriodId | Value     |
		| AS-1920              | 7000      |
		| AS-2021              | 566380.82 |
	And the Published Providers distribution period has the following profiles for funding line '1619-001'
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AS-1920              | CalendarMonth | October   | 1920 | 1          | 7000          |
		| AS-2021              | CalendarMonth | April     | 2021 | 1          | 566380.82     |
	And the Published Provider contains the following calculation results
		| TemplateCalculationId | Value |
		| 169                   |       |
		| 321                   |       |
		| 376                   |       |
		| 27                    |       |
		| 329                   |       |
		| 147                   |       |
		| 348                   |       |
		| 106                   |       |
		| 198                   |       |
		| 111                   |       |
		| 260                   |       |
		| 36                    |       |
		| 44                    |       |
		| 256                   |       |
		| 434                   |       |
		| 166                   |       |
		| 35                    |       |
		| 69                    |       |
		| 107                   |       |
		| 306                   |       |
		| 212                   |       |
		| 113                   |       |
		| 85                    |       |
		| 267                   |       |
		| 237                   |       |
		| 264                   |       |
		| 311                   |       |
		| 55                    |       |
		| 156                   |       |
		| 283                   |       |
		| 132                   |       |
		| 200                   |       |
		| 207                   |       |
		| 103                   |       |
		| 129                   |       |
		| 131                   |       |
		| 402                   |       |
		| 240                   |       |
		| 39                    |       |
		| 99                    |       |
		| 213                   |       |
		| 372                   |       |
		| 205                   |       |
		| 72                    |       |
		| 315                   |       |
		| 82                    |       |
		| 314                   |       |
		| 158                   |       |
		| 177                   |       |
		| 293                   |       |
		| 313                   |       |
		| 28                    |       |
		| 428                   |       |
		| 130                   |       |
		| 349                   |       |
		| 347                   |       |
		| 133                   |       |
		| 199                   |       |
		| 68                    |       |
		| 182                   |       |
		| 77                    |       |
		| 116                   |       |
		| 117                   |       |
		| 386                   |       |
		| 432                   |       |
		| 26                    |       |
		| 40                    |       |
		| 126                   |       |
		| 178                   |       |
		| 374                   |       |
		| 265                   |       |
		| 211                   |       |
		| 141                   |       |
		| 121                   |       |
		| 352                   |       |
		| 155                   |       |
		| 196                   |       |
		| 73                    |       |
		| 195                   |       |
		| 30                    |       |
		| 308                   |       |
		| 238                   |       |
		| 47                    |       |
		| 258                   |       |
		| 114                   |       |
		| 143                   |       |
		| 239                   |       |
		| 197                   |       |
		| 96                    |       |
		| 204                   |       |
		| 266                   |       |
		| 257                   |       |
		| 307                   |       |
		| 253                   |       |
		| 437                   |       |
		| 375                   |       |
		| 34                    |       |
		| 193                   |       |
		| 38                    |       |
		| 33                    |       |
		| 118                   |       |
		| 312                   |       |
		| 11                    |       |
		| 9                     |       |
		| 10                    |       |
		| 176                   |       |
		| 175                   |       |
		| 181                   |       |
		| 180                   |       |
		| 53                    |       |
		| 49                    |       |
		| 51                    |       |
		| 48                    |       |
		| 433                   |       |
		| 45                    |       |
		| 58                    |       |
		| 57                    |       |
		| 54                    |       |
		| 46                    |       |
		| 385                   |       |
		| 418                   |       |
		| 384                   |       |
		| 59                    |       |
		| 60                    |       |
		| 56                    |       |
		| 41                    |       |
		| 42                    |       |
		| 43                    |       |
		| 95                    |       |
		| 98                    |       |
		| 102                   |       |
		| 105                   |       |
		| 25                    |       |
		| 81                    |       |
		| 80                    |       |
		| 78                    |       |
		| 86                    |       |
		| 84                    |       |
		| 83                    |       |
		| 90                    |       |
		| 88                    |       |
		| 93                    |       |
		| 87                    |       |
		| 92                    |       |
		| 94                    |       |
		| 97                    |       |
		| 101                   |       |
		| 104                   |       |
		| 127                   |       |
		| 125                   |       |
		| 124                   |       |
		| 139                   |       |
		| 146                   |       |
		| 145                   |       |
		| 154                   |       |
		| 153                   |       |
		| 164                   |       |
		| 161                   |       |
		| 162                   |       |
		| 167                   |       |
		| 165                   |       |
		| 379                   |       |
		| 216                   |       |
		| 217                   |       |
		| 218                   |       |
		| 219                   |       |
		| 220                   |       |
		| 235                   |       |
		| 215                   |       |
		| 221                   |       |
		| 222                   |       |
		| 223                   |       |
		| 224                   |       |
		| 225                   |       |
		| 201                   |       |
		| 202                   |       |
		| 203                   |       |
		| 214                   |       |
		| 208                   |       |
		| 209                   |       |
		| 210                   |       |
		| 192                   |       |
		| 194                   |       |
		| 191                   |       |
		| 424                   |       |
		| 255                   |       |
		| 252                   |       |
		| 251                   |       |
		| 262                   |       |
		| 261                   |       |
		| 250                   |       |
		| 245                   |       |
		| 409                   |       |
		| 269                   |       |
		| 270                   |       |
		| 290                   |       |
		| 363                   |       |
		| 289                   |       |
		| 286                   |       |
		| 282                   |       |
		| 281                   |       |
		| 292                   |       |
		| 291                   |       |
		| 296                   |       |
		| 295                   |       |
		| 275                   |       |
		| 298                   |       |
		| 354                   |       |
		| 319                   |       |
		| 401                   |       |
		| 399                   |       |
		| 316                   |       |
		| 414                   |       |
		| 392                   |       |
		| 393                   |       |
		| 421                   |       |
		| 422                   |       |
		| 380                   |       |
		| 330                   |       |
		| 331                   |       |
		| 387                   |       |
		| 388                   |       |
		| 334                   |       |
		| 328                   |       |
		| 332                   |       |
		| 335                   |       |
		| 383                   |       |
		| 327                   |       |
		| 340                   |       |
		| 343                   |       |
		| 351                   |       |
		| 344                   |       |
		| 345                   |       |
		| 341                   |       |
		| 339                   |       |
		| 395                   |       |
		| 430                   |       |
		| 431                   |       |
		| 427                   |       |
		| 435                   |       |
		| 436                   |       |
		| 426                   |       |
		| 440                   |       |
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
	And the following Published Provider has been previously generated for the current specification
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
		| CalculationType | CalculationId                        | Name                                                                                         | PublishStatus |
		| Template        | 553a3ad7-a41a-4dc7-95e2-de31a59ce28d | Care Standards Eligible Students                                                             | Approved      |
		| Template        | 7528010c-3fcb-4de6-beaf-d347dbb8a9b1 | Care standards funding rate                                                                  | Approved      |
		| Template        | 19dcc38f-592c-4f53-a546-1e4b61ef8df1 | Care Standards Student funding                                                               | Approved      |
		| Template        | 2401d781-458c-456a-8ca8-ce8541c85c67 | Care Standards Lump Sum                                                                      | Approved      |
		| Template        | 1c9abadf-5c56-4e23-9ee7-225e6879e505 | Students meeting programme uplift 20% criteria                                               | Approved      |
		| Template        | 85d61ce7-e406-4eb0-a21f-367c1c5bc4e6 | Uplift per student at 20% national rate                                                      | Approved      |
		| Template        | af9884e3-44d8-487c-92bc-0e6f79ea1db9 | Length of programme                                                                          | Approved      |
		| Template        | 9cfc3fdf-d785-45ef-a1b6-364b5d1f3444 | Large programme funding uplift at 20% National rate                                          | Approved      |
		| Template        | 703a342f-70c2-4bcf-bed4-a3b83e1980ac | Students meeting programme uplift 10% criteria                                               | Approved      |
		| Template        | 99947b7d-4e46-43ab-829f-9fae386970c6 | Uplift per student at 10% national rate                                                      | Approved      |
		| Template        | bc9e531f-0c47-4457-8771-8316c70993e9 | Large programme funding uplift at 10% National rate                                          | Approved      |
		| Template        | 6157c1a7-cf0c-46f0-9ff2-21c8a85835b8 | Students R46 total for RATIO                                                                 | Approved      |
		| Template        | ac4fdeac-5874-4b48-b2ed-d95ebf66b8cf | Students R04 Total for RATIO                                                                 | Approved      |
		| Template        | d3b4a791-a6fe-4ae4-b1a0-334bad7638e8 | R46 to R14 Ratio                                                                             | Approved      |
		| Template        | 18c4958a-03b5-4bad-8100-f3cb08c57c23 | R04 to R14 Ratio                                                                             | Approved      |
		| Template        | 56641418-45fa-4417-ad15-2c266d526d2a | Ratio Exception                                                                              | Approved      |
		| Template        | 841be5a6-8184-4d81-878d-e53a9fdfd851 | ILR R14 Full Year Students                                                                   | Approved      |
		| Template        | e39a3f89-c5f9-4eb2-8069-7532b8cb19ba | ILR Rolling 12 months Students                                                               | Approved      |
		| Template        | 41c36a55-0702-47fe-bdc3-14e102ed10a5 | ILR Hybrid Students                                                                          | Approved      |
		| Template        | 523063b2-56fb-491e-b433-2cbbc8dc2f66 | Autumn Census                                                                                | Approved      |
		| Template        | 03b1b69d-62e9-4465-aafb-8386586b61ea | R46 Students                                                                                 | Approved      |
		| Template        | de27e3f0-ccf1-458e-ae87-44dac0868c6f | R04 Students                                                                                 | Approved      |
		| Template        | eadf49dc-c4b0-47fb-a0f6-b069446b61e5 | Ratio                                                                                        | Approved      |
		| Template        | 1c3dcf69-6f11-439c-b5b4-2f7a36c051c3 | R06 Students                                                                                 | Approved      |
		| Template        | 9890b97b-4241-449a-9d50-31e4fab8d9be | Academy Estimates                                                                            | Approved      |
		| Template        | 1c864d19-fb3b-4497-bc07-416d98438732 | Year 1 Business Case Students                                                                | Approved      |
		| Template        | 5a758ba8-f24b-4d44-92a5-214487325e73 | Year 2 Business Case Students                                                                | Approved      |
		| Template        | a7c3d41f-56d5-44ac-bf99-d80537c6f70a | Lagged students                                                                              | Approved      |
		| Template        | 1ee652fa-1f8b-48f2-81b7-7f66fd05e1f8 | Student Number Methodology Used                                                              | Approved      |
		| Template        | d3cedf1c-5c3e-45db-b9ae-582eca959ce6 | Total Baseline (lagged) Student Number                                                       | Approved      |
		| Template        | 3aa8c250-13ab-45a2-bf8f-8bb9895d5611 | Exceptional Variations to Baseline Student Number                                            | Approved      |
		| Template        | 689900d0-7e65-4092-8e9a-aca6e8f6469e | Band 4a students full year data                                                              | Approved      |
		| Template        | af0a982b-519f-4bce-b422-73d7493fdaf8 | Band 4b students full year data                                                              | Approved      |
		| Template        | 2dad9dca-4c9e-48c5-97d5-0fbfbb526cbf | Band 5 students full year data                                                               | Approved      |
		| Template        | f8be8202-09b6-43a1-8b26-4b0352f559c9 | Total Band 4 students full year data                                                         | Approved      |
		| Template        | f33a870c-1723-4620-b961-6bf919b83219 | Band 3 students full year data                                                               | Approved      |
		| Template        | fdf310a4-97d4-4fa7-839c-7f85d41c2eb4 | Band 2 students full year data                                                               | Approved      |
		| Template        | f92211c7-2a92-4372-bfdd-2c98857c3809 | Band 1 students full year data                                                               | Approved      |
		| Template        | 13d185c1-d927-4898-9334-9ab07a50b181 | Total students full year data                                                                | Approved      |
		| Template        | fa729035-54ca-4c47-8ce9-b4c9613a3382 | Total Funded Student number or Academy Estimates                                             | Approved      |
		| Template        | f5b823a2-3492-44ae-8e7c-cfce96ad4c64 | Band 5 Student proportion                                                                    | Approved      |
		| Template        | b81eae80-560c-44fd-a330-7f3ef94e0a9f | Band 9 Funded students current year                                                          | Approved      |
		| Template        | 683c125a-3b98-40ab-b667-a11ad4b17919 | Band 8 Funded students current year                                                          | Approved      |
		| Template        | ed30f797-2bc6-412c-a0c7-01a79f33c753 | Band 7 Funded students current year                                                          | Approved      |
		| Template        | 44f6c2bc-9e2c-4794-9751-67480cabed62 | Band 6 Funded students current year                                                          | Approved      |
		| Template        | cb06f25c-c0f0-4af1-9d6a-e06607e81b65 | Band 5 Funded Students                                                                       | Approved      |
		| Template        | ec48dff0-7598-4bcc-bce4-afd916438b58 | Total T-Level Funded students                                                                | Approved      |
		| Template        | 0389cb82-f910-468f-8421-c962ad47b012 | Band 5 students less T-Level students                                                        | Approved      |
		| Template        | 89632558-d0c2-4b3b-9d22-6031e75d81a6 | Band 5 Rate current year                                                                     | Approved      |
		| Template        | abcd282c-d508-4919-ad57-faffaf6de204 | Band 5 Student funding                                                                       | Approved      |
		| Template        | 2e5ec4f8-26ca-48ef-9777-dfe8221ce042 | Band 4a student proportion                                                                   | Approved      |
		| Template        | 7a5ed641-41e3-439e-8c65-fedb67cb500d | Band 4b student proportion                                                                   | Approved      |
		| Template        | 9ba52699-283f-4ea8-bcda-c571399a6778 | Band 4 total proportion                                                                      | Approved      |
		| Template        | c967dd74-156c-495e-ae0e-b519bed7d164 | Band 4 Funded Students                                                                       | Approved      |
		| Template        | 987b226a-d1dc-4b79-ab37-f4613fa4dc9e | Band 4 rate current year                                                                     | Approved      |
		| Template        | ab823493-460a-43a5-a6d7-e489ae35b11f | Band 4 Student Funding                                                                       | Approved      |
		| Template        | be1ed4a6-676e-4b0b-ab41-f14270b2a19d | Band 3 student proportion                                                                    | Approved      |
		| Template        | e0ab7d11-b550-42a7-a2a0-9d5bb76df796 | Band 3 Funded students                                                                       | Approved      |
		| Template        | 7adf1bdc-cec2-4452-b0a4-a95a9735a82f | Band 3 rate current year                                                                     | Approved      |
		| Template        | 31a31d04-64e6-4dec-8482-c3d24c9c7565 | Band 3 Student funding                                                                       | Approved      |
		| Template        | 745f0997-c675-4bc9-be95-31953c5798e7 | Band 2 student proportion                                                                    | Approved      |
		| Template        | a1e3b3e7-de49-43ff-89a7-932a8c13ccbc | Band 2 Funded students                                                                       | Approved      |
		| Template        | 49471705-7aaf-4308-9a2b-ef316cead192 | Band 2 rate current year                                                                     | Approved      |
		| Template        | 72a8ac10-bc57-4057-8907-a0dbda260f5c | Band 2 Student funding                                                                       | Approved      |
		| Template        | 6024bf9d-246a-4d54-ad0e-94ff38ec22bd | Band 1 FTE full year data                                                                    | Approved      |
		| Template        | ea82c1ae-aa28-4a14-b5c9-e5aa7dd96100 | Band 1 FTE Funded students                                                                   | Approved      |
		| Template        | 9895b991-9933-40be-93bb-5f77b89ee5ba | Band 1 Student proportion                                                                    | Approved      |
		| Template        | f3332d77-a051-40ba-a883-f0fc134713ce | Band 1 FTE Student Funding                                                                   | Approved      |
		| Template        | 2a81fe81-18f3-4d11-bab0-8c0a746e279a | Band 1 Funded Students                                                                       | Approved      |
		| Template        | dc5b2ffe-8d48-452d-87bc-29e483b86b64 | Band 9 Rate current year                                                                     | Approved      |
		| Template        | f9c11de5-9e6e-4b73-9986-ab067a7bc0ab | T-Level Band 9 Student funding                                                               | Approved      |
		| Template        | 6c3f69c1-9596-49c7-af84-cb4451033a79 | Band 8 Rate current year                                                                     | Approved      |
		| Template        | 66f880f2-9ee1-4f42-a8fe-3d1f3589351e | T-Level Band 8 Student funding                                                               | Approved      |
		| Template        | 5acf6d2f-b1c0-4dae-a170-8ebf1aae6097 | Band 7 Rate current year                                                                     | Approved      |
		| Template        | de4925bc-b320-42b5-909a-f545450ef16b | T-Level Band 7 Student funding                                                               | Approved      |
		| Template        | 0d3756e6-288b-4891-aaef-a79cf57b48b9 | Band 6 Rate current year                                                                     | Approved      |
		| Template        | d53204a5-cd76-45f2-8bcb-a6a078a788cc | T-Level Band 6 Student funding                                                               | Approved      |
		| Template        | 3a88a844-f40e-4f55-b0c2-0079c0a5f064 | Total Mainstream band funded students                                                        | Approved      |
		| Template        | ca229fec-a815-417c-b680-8dfa0bc7fff3 | Level 3 Maths and English One Year instances per student                                     | Approved      |
		| Template        | 5cee8980-d752-4c03-9cde-1aa1409195fa | Total Student Funded students                                                                | Approved      |
		| Template        | 2ac24e50-4203-4c23-b510-94dea87d42e8 | Level 3 Maths and English One Year number of instances                                       | Approved      |
		| Template        | af4934a6-19db-4dae-bfcb-ea395f9d4ef1 | Level 3 Maths and English One Year Rate                                                      | Approved      |
		| Template        | 3018962a-6355-4dbb-aa99-e141e4763a51 | Level 3 Maths and English One Year Funding Total                                             | Approved      |
		| Template        | 95583d82-0803-4797-9cca-7ae036f48924 | Level 3 Maths and English Two Year instances per student                                     | Approved      |
		| Template        | 60441015-d128-4e8c-a442-5603571d7d9a | Level 3 Maths and English Two Year number of instances                                       | Approved      |
		| Template        | 57506711-b246-446a-bd21-34a091a81bcf | Level 3 Maths and English Two Year Rate                                                      | Approved      |
		| Template        | 90c4af52-b6ff-4a32-a4ba-7de2aa259873 | Level 3 Maths and English Two Year Funding Total                                             | Approved      |
		| Template        | 436e47cd-4cc4-4f13-a0ca-8b5ca5b580d9 | Student Funding                                                                              | Approved      |
		| Template        | 9d75300c-c3fd-45f1-9aaf-329e4a656d1d | Retention Factor                                                                             | Approved      |
		| Template        | d75578b7-e178-4b6e-8339-63693af83d4e | Student Funding with retention factor applied                                                | Approved      |
		| Template        | 0f26909b-e18a-408a-95bd-284d40012cd4 | Retention Factor Adjustment                                                                  | Approved      |
		| Template        | b945fe90-d97a-4d5c-a356-04eba8f64593 | Programme Cost Weighting                                                                     | Approved      |
		| Template        | fc28a81d-ac92-44e4-9019-749d0bbb3537 | Student funding with retention factor and programme cost weighting applied                   | Approved      |
		| Template        | a0c9f82b-d505-4706-b268-17eca2cc57c2 | Programme cost weighting funding adjustment                                                  | Approved      |
		| Template        | b4bb26dd-e7a5-4237-a788-b899848a18cb | Student funding with retention programme cost weighting and L3 Maths and English             | Approved      |
		| Template        | da4a1622-3ee1-4da2-b234-399a7f670f99 | Block 1 Factor                                                                               | Approved      |
		| Template        | a6747aa9-0c9f-484b-8014-27d95e5c1afd | Block 1 Funding payment                                                                      | Approved      |
		| Template        | 80441694-000f-4e73-8c93-5916a9a3c86d | Number of qualifying care leaver students                                                    | Approved      |
		| Template        | 043f26b9-04c4-44ab-8f30-02ad59651c3c | Care leaver rate per qualifying student                                                      | Approved      |
		| Template        | ebb3445a-5273-4062-87ae-ecc415060e84 | Care leaver funding                                                                          | Approved      |
		| Template        | 83f74d71-3681-4931-9dfb-e55c3b807c47 | Instances per student                                                                        | Approved      |
		| Template        | 540e3af3-5913-4124-a4bf-a7112f6c2ceb | Total funded instances current year                                                          | Approved      |
		| Template        | 25643e61-1bd2-46cf-9ee6-85214cde47bc | Student instances attracting the lower rate                                                  | Approved      |
		| Template        | bc9af8f8-1cab-4860-8f0f-3460dae55105 | Disadvantage Block 2 funding lower rate                                                      | Approved      |
		| Template        | 67e6521a-42e4-4226-a3c7-5033e4861a54 | Disadvantage Block 2 lower funding                                                           | Approved      |
		| Template        | 2f011352-0a2f-4ce6-9a16-7014a1037bd3 | Students attracting the disadvantage block 2 FTE Rate                                        | Approved      |
		| Template        | 0324416b-2724-4996-8eb1-a04f799ec1d9 | Disadvantage block 2 higher rate                                                             | Approved      |
		| Template        | f224bbde-85f8-4981-9908-d01e8fb62787 | Band 1 Funded student instances                                                              | Approved      |
		| Template        | 0debb6c2-f189-40c9-8420-b43147a7bdb3 | Disadvantage Block 2 FTE Funding                                                             | Approved      |
		| Template        | 2cf180d6-94b6-436c-b20d-ab458b772db0 | Disadvantage Block 2 T-Level rate                                                            | Approved      |
		| Template        | 4cd7f32e-16cf-41a0-b766-648f6f686eed | Students attracting the T-Level rate                                                         | Approved      |
		| Template        | 67974b2c-7068-4920-a8de-632b7f428148 | Disadvantage Block 2 T-Level Funding                                                         | Approved      |
		| Template        | eeb98d36-dea1-4fa4-a692-67c1c218c82b | Students attracting disadvantage block 2 higher rate                                         | Approved      |
		| Template        | b16e19ee-352b-4959-978c-0789f0530c0e | Disadvantage block 2 Higher Funding                                                          | Approved      |
		| Template        | 01b96a4a-688e-4396-8904-f882f09fbb62 | Minimum Funding Top up                                                                       | Approved      |
		| Template        | c5a3b0a2-fc1e-4a7c-8be8-fea57cb5d71d | Total large programme students                                                               | Approved      |
		| Template        | da7ec136-2fb9-4778-9e40-78de3fe2c73f | Programme funding without area cost allowance applied                                        | Approved      |
		| Template        | 65c5fbf0-5699-4d9f-a303-1fc55e41df24 | Area cost factor                                                                             | Approved      |
		| Template        | d398f89d-be0c-4834-8b90-5811e207a558 | Programme funding with area cost allowance applied                                           | Approved      |
		| Template        | 4a7b9179-b264-4495-867e-5de3fd5bbed7 | Area cost allowance                                                                          | Approved      |
		| Template        | 27b4bbf3-7aef-4e62-bc55-8d9c954cfc65 | Band 5 Students not meeting condition of funding                                             | Approved      |
		| Template        | 15d349b5-584c-4bfe-9650-42e9476a0175 | Band 4 Students not meeting condition of funding                                             | Approved      |
		| Template        | 56a9b23b-87ba-4853-91d7-b01c95637fdd | Band 3 Students not meeting condition of funding                                             | Approved      |
		| Template        | 3068f1c5-15fc-46ca-b688-61f8c7910551 | Band 2 Students not meeting condition of funding                                             | Approved      |
		| Template        | d574bee3-d494-4733-b2f4-febcaa69dbe2 | Band 1 Students not meeting condition of funding                                             | Approved      |
		| Template        | 76fe5221-e365-4d4e-a96c-7c5fe97c1b86 | Band 5 National Funding Lagged Rate                                                          | Approved      |
		| Template        | b3f4e188-f938-4f69-8385-725928f51021 | Band 4 National Funding Lagged Rate                                                          | Approved      |
		| Template        | 3f30d2c4-abf3-4839-b1db-d97982b2e217 | Band 3 National Funding Lagged Rate                                                          | Approved      |
		| Template        | 74c1002d-e3e9-450e-8e2d-9b951c050765 | Band 2 National Funding Lagged Rate                                                          | Approved      |
		| Template        | e1bf7483-9e52-44e3-aba7-0c72e9e20a5c | Band 1 FTE not meeting condition of funding                                                  | Approved      |
		| Template        | d9889c0e-498a-42af-b89d-01518040f7e9 | Total Students not meeting Condition of funding                                              | Approved      |
		| Template        | 734604c6-c68c-4b16-8d12-4f1071f79ecb | Band 5 Funding for CoF Non Compliant students                                                | Approved      |
		| Template        | c4eca2d2-0596-4e37-839a-2315773f3514 | Band 4 Funding for CoF Non Compliant students                                                | Approved      |
		| Template        | 70f45ad0-7119-4d02-9acd-238779df6735 | Band 3 Funding for CoF Non Compliant students                                                | Approved      |
		| Template        | c4bcbc74-1750-4654-888e-9bff582bb06b | Band 2 Funding for CoF Non Compliant students                                                | Approved      |
		| Template        | 7b2c84a6-62f3-4f82-a1d1-b235b24b6e94 | Band 1 FTE Funding for CoF non compliant students                                            | Approved      |
		| Template        | 964f3e3b-f89c-4510-8856-72c63986a41e | Band 5 Students excluding 19+ Full year data                                                 | Approved      |
		| Template        | 5804bc28-4301-41b0-a622-759f2c6be887 | Band 4 Students excluding 19+ full year data                                                 | Approved      |
		| Template        | 47fbb66a-ab86-4edf-a109-280a5ba6aacb | Band 3 Students excluding 19+ full year data                                                 | Approved      |
		| Template        | 2886704a-8534-4740-802d-831498acfc5e | Band 2 Students excluding 19+ full year data                                                 | Approved      |
		| Template        | a159261d-cc3b-49d8-9609-da5eb164a9bf | Band 1 Students excluding 19+ full year data                                                 | Approved      |
		| Template        | 2b8aae02-5d4c-49fa-8158-6f069c395e65 | Data Source                                                                                  | Approved      |
		| Template        | 857d0cbd-1ba4-4593-9a93-1b3ad58f97d4 | Band 1 FTE excluding 19+ full year data                                                      | Approved      |
		| Template        | 0e81d9bf-1da8-40f9-b8fb-73e1ed6e6c0a | Total Students Excluding 19+ Full year data                                                  | Approved      |
		| Template        | df2d07f9-c90c-4699-8f7a-7fc07d0644e2 | Band 5 Funding Full Year Students excluding 19+                                              | Approved      |
		| Template        | 5806c9dd-7066-4fcc-adc0-da062dc2ef4e | Band 4 Funding Full Year Students excluding 19+                                              | Approved      |
		| Template        | 091cba13-2258-48b2-a85b-4ae55be28c29 | Band 3 Funding Full Year Students excluding 19+                                              | Approved      |
		| Template        | 58c66937-8093-4168-bf4f-eccb2e3aacbf | Band 2 Funding Full Year Students excluding 19+                                              | Approved      |
		| Template        | d544c44e-b6b5-431e-ae4a-5c8ed92dae65 | Band 1 FTE Funding Full Year Students excluding 19+                                          | Approved      |
		| Template        | bccbb3b8-0abd-4194-b659-734803ec7fe4 | Total Funding Full Year excluding 19+                                                        | Approved      |
		| Template        | dad22e0a-1025-45b8-980a-44eb30700e88 | Condition of funding tolerance                                                               | Approved      |
		| Template        | 7995d48f-935f-4190-bf37-8102a0538f2e | Condition of funding adjustment above tolerance                                              | Approved      |
		| Template        | f73b383d-9147-4cb8-a277-49a1e90820cf | Condition of funding Reduced Rate                                                            | Approved      |
		| Template        | fff084e0-24ee-47e6-9b3c-eddca35f2e24 | Total funding for CoF non compliant students                                                 | Approved      |
		| Template        | c9f6dafe-d65f-4bc3-b468-af96a07723ae | Tolerance applied to Total Funding Full Year Students excluding 19+                          | Approved      |
		| Template        | 2fa7ba90-686a-4f46-9078-e913e1eb7e7a | Condition of funding adjustment                                                              | Approved      |
		| Template        | d15867ca-0e79-457d-a964-d1f935e3ab7c | Offset high value courses for school and college leavers in year programme funding           | Approved      |
		| Template        | 14a82c4d-802d-4f67-926f-81de92cab917 | Financial Disadvantage instances per student                                                 | Approved      |
		| Template        | ea2b0af5-85f1-4b3b-9a5c-93df58f847ab | Funded Students Lagged Or High Needs                                                         | Approved      |
		| Template        | 28d1745f-f977-4a3b-8e96-6a97d782c78c | Financial Disadvantage Number of instances                                                   | Approved      |
		| Template        | e3f049d6-22fa-435e-88e9-093fecc5eca1 | Financial Disadvantage instance Rate                                                         | Approved      |
		| Template        | 39378dbe-c5e5-4882-9a46-a4f8606c5d8e | Financial Disadvantage Funding                                                               | Approved      |
		| Template        | 12a78abb-070b-45fe-8432-ea38d4d09232 | Student Costs Travel Instances per student                                                   | Approved      |
		| Template        | e0b79abc-5609-4236-a049-d5d28b549d37 | Student Costs Travel Number of instances                                                     | Approved      |
		| Template        | d7265454-922e-4dc5-83f9-d82983bf76be | Student Costs Travel Funding instance Rate                                                   | Approved      |
		| Template        | 2587e2ec-0280-4e3f-9161-fb5e62932468 | Student Costs Travel Funding                                                                 | Approved      |
		| Template        | cf051bc2-16e2-465d-a71f-a49118209762 | Student Costs Industry placement Number of instances                                         | Approved      |
		| Template        | 5070f082-8f7a-4648-aad3-dd31387acbed | Student Costs Industry Placements rate                                                       | Approved      |
		| Template        | f1148e18-d82b-4ef6-9320-b29104a89e4d | Student Costs Industry Placements funding                                                    | Approved      |
		| Template        | 3d1dbda3-ee02-45bd-8dc5-d76b1fa666cc | Bursary adjustment in respect of Free meals                                                  | Approved      |
		| Template        | 6727fea3-d6ca-4e2d-8618-b209a919eeda | Exceptional adjustment to Discretionary Bursary Fund                                         | Approved      |
		| Template        | b2fd937f-1367-420a-a25d-2379fd71a410 | Transition Lower Limit                                                                       | Approved      |
		| Template        | df678971-68b4-4c6b-88e4-362718c71a42 | Transition upper limit                                                                       | Approved      |
		| Template        | 4a4745b7-4241-4b63-99ee-4f0c0482d6fd | Baseline for Discretionary bursary Transition fixed 2019 to 2020                             | Approved      |
		| Template        | 32e5bf02-1e9f-4b98-a06a-e4f49c531d23 | Discretionary bursary transition adjustment                                                  | Approved      |
		| Template        | d851dc56-9e36-4afc-8551-179055e1995c | Residential Bursary Fund                                                                     | Approved      |
		| Template        | 93d68f09-a0c2-42fe-baf9-0276433e8de7 | Residential Support Scheme                                                                   | Approved      |
		| Template        | 94a3b3e4-c24a-438e-a6c4-23935da28bf3 | Free Meals Students                                                                          | Approved      |
		| Template        | 626cc08a-25a0-4f4f-a373-6b97afbf419c | Total Students full year data for use in free meals                                          | Approved      |
		| Template        | 5e5d1249-e8da-44e7-8aa7-a96024e93c2d | Proportion of students on Free Meals                                                         | Approved      |
		| Template        | 95e94909-df15-446e-bbb5-2a796353d29b | Total Students Funded for Free Meals for Current Year                                        | Approved      |
		| Template        | 01fb7c9d-ec74-4f43-b404-c918f7fe7fe5 | Free Meals Students attracting the Higher Rate                                               | Approved      |
		| Template        | 5419db0b-d463-4c97-a0db-2c2f679a0e24 | Free Meals Higher Rate                                                                       | Approved      |
		| Template        | be43e79d-e36d-49f9-8fd8-8f2c7bc3d0bd | Free meals Higher Funding                                                                    | Approved      |
		| Template        | ecf19c2a-6373-429c-a8de-2f4a7322e84f | Free Meals Students attracting the Lower Rate                                                | Approved      |
		| Template        | 881ea2c3-997e-463a-b316-398bd9d833ba | Free Meals Lower Rate                                                                        | Approved      |
		| Template        | a1f45664-cd06-4572-a580-f5afd1996b89 | Free Meals Lower Funding                                                                     | Approved      |
		| Template        | 2d5d37f5-faea-48b4-b033-a60a2544b031 | Free Meals Students attracting the FTE Rate                                                  | Approved      |
		| Template        | d7c65add-6884-45c8-8353-c3857f98b3cf | Free Meals FTE Funding                                                                       | Approved      |
		| Template        | 34836ed7-3953-4c3f-8f56-1d410c716fdf | Free meals administration cost                                                               | Approved      |
		| Template        | 18a9f732-944a-4d23-b713-43be830ea9b6 | Free Meals Exceptional Adjustment                                                            | Approved      |
		| Template        | 0b6a65dc-8449-4643-bf30-07690dbeee1c | Disadvantage Accommodation Costs                                                             | Approved      |
		| Template        | 02b55a1e-685d-4524-a0ff-097a795932c5 | Industry placements T-Level Rate                                                             | Approved      |
		| Template        | 99ca3111-a417-4bbf-aa9e-006c697375bf | Industry Placements T-Level Funding                                                          | Approved      |
		| Template        | ca034370-fb4c-4073-8b05-97fe7b5f0857 | Industry Placements Capacity and Delivery Qualifying Students                                | Approved      |
		| Template        | 648948e3-58e6-4592-8424-adeb24fc0581 | Industry Placements Capacity and Delivery rate                                               | Approved      |
		| Template        | d20f9bed-4032-491c-9300-aeb41915dcf1 | Industry Placements Capacity and Delivery Funding                                            | Approved      |
		| Template        | 1ead4132-fbb5-4bc9-8506-33b08e10e587 | Alternative Completions - Sporting Excellence                                                | Approved      |
		| Template        | 5b59fdd2-60ab-4127-822b-fa81c3962f8d | Alternative Completions - Sea Fishing                                                        | Approved      |
		| Template        | f99102a3-5024-48db-b9e4-db1fc9a69043 | Post Opening Grant - Per Pupil Resources                                                     | Approved      |
		| Template        | 7ba82116-0fcc-4e7d-9f88-ae21744d2651 | Post Opening Grant - Leadership Diseconomies                                                 | Approved      |
		| Template        | 62156712-8463-4630-a76f-13b461711ab9 | Start Up Grant Part A                                                                        | Approved      |
		| Template        | 7d68e00b-b7c2-477f-b4fe-5b8cff4e229a | Start Up Grant Part B                                                                        | Approved      |
		| Template        | deb8cc38-c170-4567-8f14-20b5dcad8922 | Current year Total Programme funding per student - SPI                                       | Approved      |
		| Template        | 90d9f82d-0524-4db8-9cfa-f14cabceda02 | 16-19 High needs students                                                                    | Approved      |
		| Template        | b7b2ff6f-a347-4853-b051-fc128913776c | 19-24 High Needs students                                                                    | Approved      |
		| Template        | 3f437067-7ef1-481c-a631-db5d33031241 | R06 16-19 High needs students                                                                | Approved      |
		| Template        | 5d9983ac-8748-4780-95d9-16f877c299da | R06 19-24 High needs students                                                                | Approved      |
		| Template        | 9c6540c4-5d9a-4655-a24d-560ead889e68 | Total R06 High Needs Student Number                                                          | Approved      |
		| Template        | 45550274-958a-4042-be9d-775a996946d4 | Total High Needs Students                                                                    | Approved      |
		| Template        | 03d0227b-7a19-456f-b0fa-23048026e96a | High Needs Rate                                                                              | Approved      |
		| Template        | 17df4975-f739-4574-8f06-6aafd3a28ad8 | 16-19 High Needs Student Proportion                                                          | Approved      |
		| Template        | 0fe3fa89-8361-46d9-b584-e415dfc5675a | 19-24 High Needs Student proportion                                                          | Approved      |
		| Template        | fadc2ef0-417c-4cde-a8d7-04cad69647b1 | Exceptional variations to High Needs student number                                          | Approved      |
		| Template        | de1bd71b-29e0-4f48-b4d2-71384e92b9f5 | High Needs Element 2 Student Funding                                                         | Approved      |
		| Template        | 8159aafd-095a-4e3d-a580-d005a5ad52d4 | Advanced Maths Baseline Students                                                             | Approved      |
		| Template        | 7ad71f61-14aa-49b5-8101-0885d1767f42 | Advanced Maths Eligible Students                                                             | Approved      |
		| Template        | ffc2ed9a-98ee-44e0-af6f-b782b9bcd76a | Advanced Maths Rate                                                                          | Approved      |
		| Template        | 8953b6c3-6fed-4a27-b2ed-9c854b5f5a90 | Advanced Maths Eligible Students minus baseline students                                     | Approved      |
		| Template        | 518951bb-0002-4ddc-80ce-8ee039398d91 | Advanced Maths Premium Funding                                                               | Approved      |
		| Template        | e32aeb34-427f-404c-9867-43a178d991c3 | Number of qualifying High Value Course Students                                              | Approved      |
		| Template        | d34132de-0abf-4126-b552-9d65024a450f | High Value Course Premium Rate                                                               | Approved      |
		| Template        | 3bbfa0fc-0380-4903-b57d-804176d9f65e | High Value Course Premium Funding                                                            | Approved      |
		| Template        | 61fd3afc-d74a-450b-a6c3-0858260112df | Teachers Pension Annual Payments for previous full financial year                            | Approved      |
		| Template        | 91f7a24a-67e7-4743-a727-28ada5333bc2 | Employer contribution rate included                                                          | Approved      |
		| Template        | 0aa77eef-37cc-4879-809b-5f3a204af156 | Employer contribution rate previous FY                                                       | Approved      |
		| Template        | 91841fdd-7f62-4f19-bc07-f72f45c53ae2 | OBR Wage growth forecast previous Year                                                       | Approved      |
		| Template        | 25d5b084-4274-4edb-ae49-699197eb6eee | Teachers Pension Annual payments including Employer contributions increase for previous year | Approved      |
		| Template        | c1575687-5bb1-4604-a524-dcbed3a3e3f0 | Teachers Pension annual pay with increase with OBR previous year applied                     | Approved      |
		| Template        | b99d7944-1874-497b-90ff-5590039a7d96 | OBR Wage Growth Forecast current year                                                        | Approved      |
		| Template        | 5a628ba1-94ac-440e-b78d-296f5f2b5da7 | Teachers Pension uplift for previous FY OBR wage growth                                      | Approved      |
		| Template        | c8204022-f046-4606-ad8d-d9683bfb25a6 | Teacher Pension uplift for current FY OBR wage growth                                        | Approved      |
		| Template        | d89bf663-2ed8-44d2-935c-b0cf9a9a84f2 | Teachers Pension Revised Annual Cost                                                         | Approved      |
		| Template        | 83a4a011-ac69-4213-95e3-395aa6a6bfe0 | Difference between Teachers Pension available FY payments and Revised Annual Cost            | Approved      |
		| Template        | 8643d35e-e326-4628-9e52-96908c53e2d9 | Maths Top Up                                                                                 | Approved      |
		| Template        | b09f67a1-343f-4359-ae43-7e324858381d | High value courses for school and college leavers eligible students                          | Approved      |
		| Template        | e87eae67-5495-451e-a531-9abebd2f23e0 | High value courses for school and college leavers baseline                                   | Approved      |
		| Template        | ac4d0e5d-1afe-410f-a65b-4913ba85ce92 | High value courses for school and college leavers students above baseline                    | Approved      |
		| Template        | 8e0335fd-2d7b-44ea-9c20-ffa7803cf75b | High value courses for school and college leavers rate                                       | Approved      |
		| Template        | 6d5a0965-470e-4794-a0ac-4e1d80a18736 | High value courses for school and college leavers previously funded                          | Approved      |
		| Template        | e5105ca6-3ef2-443e-830a-b915cb8a42dd | High value courses for school and college leavers additional students                        | Approved      |
		| Template        | c6ab4f61-cd95-4596-ac74-47ce66ff997b | High value courses for school and college leavers uplift funding                             | Approved      |
		| Template        | bb72366d-e7ab-40f9-9fc6-f62ec41c9cc9 | LA Maintained Special School Bursary Funding                                                 | Approved      |
	And calculations exists
		| Value        | Id                                   |
		| 1437.5       | 01b96a4a-688e-4396-8904-f882f09fbb62 |
		| 275          | 02b55a1e-685d-4524-a0ff-097a795932c5 |
		| 480          | 0324416b-2724-4996-8eb1-a04f799ec1d9 |
		| 117.6237624  | 0389cb82-f910-468f-8421-c962ad47b012 |
		| 6000         | 03d0227b-7a19-456f-b0fa-23048026e96a |
		| 480          | 043f26b9-04c4-44ab-8f30-02ad59651c3c |
		| 23.6         | 0aa77eef-37cc-4879-809b-5f3a204af156 |
		| 4363         | 0d3756e6-288b-4891-aaef-a79cf57b48b9 |
		| 101          | 0e81d9bf-1da8-40f9-b8fb-73e1ed6e6c0a |
		| -4006.545735 | 0f26909b-e18a-408a-95bd-284d40012cd4 |
		| 0.27558      | 12a78abb-070b-45fe-8432-ea38d4d09232 |
		| 101          | 13d185c1-d927-4898-9334-9ab07a50b181 |
		| Census S02   | 1ee652fa-1f8b-48f2-81b7-7f66fd05e1f8 |
		| 15476.5728   | 2587e2ec-0280-4e3f-9161-fb5e62932468 |
		| Census       | 2b8aae02-5d4c-49fa-8158-6f069c395e65 |
		| 650          | 2cf180d6-94b6-436c-b20d-ab458b772db0 |
		| 99           | 2dad9dca-4c9e-48c5-97d5-0fbfbb526cbf |
		| 0.00990099   | 2e5ec4f8-26ca-48ef-9777-dfe8221ce042 |
		| 120          | 3a88a844-f40e-4f55-b0c2-0079c0a5f064 |
		| 11200        | 3bbfa0fc-0380-4903-b57d-804176d9f65e |
		| 2700         | 3f30d2c4-abf3-4839-b1db-d97982b2e217 |
		| 500818.2168  | 436e47cd-4cc4-4f13-a0ca-8b5ca5b580d9 |
		| 2234         | 49471705-7aaf-4308-9a2b-ef316cead192 |
		| 15046.02     | 4a4745b7-4241-4b63-99ee-4f0c0482d6fd |
		| 5230.7351    | 4a7b9179-b264-4495-867e-5de3fd5bbed7 |
		| 48           | 5070f082-8f7a-4648-aad3-dd31387acbed |
		| 11400        | 518951bb-0002-4ddc-80ce-8ee039398d91 |
		| 120          | 523063b2-56fb-491e-b433-2cbbc8dc2f66 |
		| 9.5052       | 540e3af3-5913-4124-a4bf-a7112f6c2ceb |
		| 358          | 5419db0b-d463-4c97-a0db-2c2f679a0e24 |
		| 750          | 57506711-b246-446a-bd21-34a091a81bcf |
		| 2            | 5804bc28-4301-41b0-a622-759f2c6be887 |
		| 6600         | 5806c9dd-7066-4fcc-adc0-da062dc2ef4e |
		| 5061         | 5acf6d2f-b1c0-4dae-a170-8ebf1aae6097 |
		| 120          | 5cee8980-d752-4c03-9cde-1aa1409195fa |
		| 4.752        | 60441015-d128-4e8c-a442-5603571d7d9a |
		| 210          | 648948e3-58e6-4592-8424-adeb24fc0581 |
		| 1.01         | 65c5fbf0-5699-4d9f-a303-1fc55e41df24 |
		| 1            | 689900d0-7e65-4092-8e9a-aca6e8f6469e |
		| 5584         | 6c3f69c1-9596-49c7-af84-cb4451033a79 |
		| 2133         | 74c1002d-e3e9-450e-8e2d-9b951c050765 |
		| 817          | 7528010c-3fcb-4de6-beaf-d347dbb8a9b1 |
		| 4000         | 76fe5221-e365-4d4e-a96c-7c5fe97c1b86 |
		| 0.00990099   | 7a5ed641-41e3-439e-8c65-fedb67cb500d |
		| 51           | 7ad71f61-14aa-49b5-8101-0885d1767f42 |
		| 2827         | 7adf1bdc-cec2-4452-b0a4-a95a9735a82f |
		| 32           | 8159aafd-095a-4e3d-a580-d005a5ad52d4 |
		| 0.07921      | 83f74d71-3681-4931-9dfb-e55c3b807c47 |
		| 838          | 85d61ce7-e406-4eb0-a21f-367c1c5bc4e6 |
		| 179          | 881ea2c3-997e-463a-b316-398bd9d833ba |
		| 19           | 8953b6c3-6fed-4a27-b2ed-9c854b5f5a90 |
		| 4188         | 89632558-d0c2-4b3b-9d22-6031e75d81a6 |
		| 400          | 8e0335fd-2d7b-44ea-9c20-ffa7803cf75b |
		| 3564         | 90c4af52-b6ff-4a32-a4ba-7de2aa259873 |
		| 0.012        | 91841fdd-7f62-4f19-bc07-f72f45c53ae2 |
		| 16.4         | 91f7a24a-67e7-4743-a727-28ada5333bc2 |
		| 0.0396       | 95583d82-0803-4797-9cca-7ae036f48924 |
		| 99           | 964f3e3b-f89c-4510-8856-72c63986a41e |
		| 3455         | 987b226a-d1dc-4b79-ab37-f4613fa4dc9e |
		| 419          | 99947b7d-4e46-43ab-829f-9fae386970c6 |
		| 0.01980198   | 9ba52699-283f-4ea8-bcda-c571399a6778 |
		| 0.992        | 9d75300c-c3fd-45f1-9aaf-329e4a656d1d |
		| 16697.84023  | a0c9f82b-d505-4706-b268-17eca2cc57c2 |
		| 120          | a7c3d41f-56d5-44ac-bf99-d80537c6f70a |
		| 8209.90099   | ab823493-460a-43a5-a6d7-e489ae35b11f |
		| 492608.3168  | abcd282c-d508-4919-ad57-faffaf6de204 |
		| 1            | af0a982b-519f-4bce-b422-73d7493fdaf8 |
		| 375          | af4934a6-19db-4dae-bfcb-ea395f9d4ef1 |
		| 2            | af9884e3-44d8-487c-92bc-0e6f79ea1db9 |
		| 4562.496     | b16e19ee-352b-4959-978c-0789f0530c0e |
		| 7523.01      | b2fd937f-1367-420a-a25d-2379fd71a410 |
		| 3300         | b3f4e188-f938-4f69-8385-725928f51021 |
		| 517073.5113  | b4bb26dd-e7a5-4237-a788-b899848a18cb |
		| 1.03361      | b945fe90-d97a-4d5c-a356-04eba8f64593 |
		| 0.021        | b99d7944-1874-497b-90ff-5590039a7d96 |
		| 292          | bc9af8f8-1cab-4860-8f0f-3460dae55105 |
		| 402600       | bccbb3b8-0abd-4194-b659-734803ec7fe4 |
		| 2.376237624  | c967dd74-156c-495e-ae0e-b519bed7d164 |
		| 20130        | c9f6dafe-d65f-4bc3-b468-af96a07723ae |
		| 117.6237624  | cb06f25c-c0f0-4af1-9d6a-e06607e81b65 |
		| 400          | d34132de-0abf-4126-b552-9d65024a450f |
		| 528304.2451  | d398f89d-be0c-4834-8b90-5811e207a558 |
		| 120          | d3cedf1c-5c3e-45db-b9ae-582eca959ce6 |
		| 468          | d7265454-922e-4dc5-83f9-d82983bf76be |
		| 496811.6711  | d75578b7-e178-4b6e-8339-63693af83d4e |
		| 1            | da4a1622-3ee1-4da2-b234-399a7f670f99 |
		| 517073.51    | da7ec136-2fb9-4778-9e40-78de3fe2c73f |
		| 0.05         | dad22e0a-1025-45b8-980a-44eb30700e88 |
		| 6108         | dc5b2ffe-8d48-452d-87bc-29e483b86b64 |
		| 396000       | df2d07f9-c90c-4699-8f7a-7fc07d0644e2 |
		| 22569.03     | df678971-68b4-4c6b-88e4-362718c71a42 |
		| 33.0696      | e0b79abc-5609-4236-a049-d5d28b549d37 |
		| 28           | e32aeb34-427f-404c-9867-43a178d991c3 |
		| 243          | e3f049d6-22fa-435e-88e9-093fecc5eca1 |
		| 120          | ea2b0af5-85f1-4b3b-9a5c-93df58f847ab |
		| 9.5052       | eeb98d36-dea1-4fa4-a692-67c1c218c82b |
		| 0.98019802   | f5b823a2-3492-44ae-8e7c-cfce96ad4c64 |
		| 0.5          | f73b383d-9147-4cb8-a277-49a1e90820cf |
		| 2            | f8be8202-09b6-43a1-8b26-4b0352f559c9 |
		| 120          | fa729035-54ca-4c47-8ce9-b4c9613a3382 |
		| 513509.5113  | fc28a81d-ac92-44e4-9019-749d0bbb3537 |
		| 600          | ffc2ed9a-98ee-44e0-af6f-b782b9bcd76a |
		|              | 553a3ad7-a41a-4dc7-95e2-de31a59ce28d |
		|              | 19dcc38f-592c-4f53-a546-1e4b61ef8df1 |
		|              | 2401d781-458c-456a-8ca8-ce8541c85c67 |
		|              | 1c9abadf-5c56-4e23-9ee7-225e6879e505 |
		|              | 9cfc3fdf-d785-45ef-a1b6-364b5d1f3444 |
		|              | 703a342f-70c2-4bcf-bed4-a3b83e1980ac |
		|              | bc9e531f-0c47-4457-8771-8316c70993e9 |
		|              | 6157c1a7-cf0c-46f0-9ff2-21c8a85835b8 |
		|              | ac4fdeac-5874-4b48-b2ed-d95ebf66b8cf |
		|              | d3b4a791-a6fe-4ae4-b1a0-334bad7638e8 |
		|              | 18c4958a-03b5-4bad-8100-f3cb08c57c23 |
		|              | 56641418-45fa-4417-ad15-2c266d526d2a |
		|              | 841be5a6-8184-4d81-878d-e53a9fdfd851 |
		|              | e39a3f89-c5f9-4eb2-8069-7532b8cb19ba |
		|              | 41c36a55-0702-47fe-bdc3-14e102ed10a5 |
		|              | 03b1b69d-62e9-4465-aafb-8386586b61ea |
		|              | de27e3f0-ccf1-458e-ae87-44dac0868c6f |
		|              | eadf49dc-c4b0-47fb-a0f6-b069446b61e5 |
		|              | 1c3dcf69-6f11-439c-b5b4-2f7a36c051c3 |
		|              | 9890b97b-4241-449a-9d50-31e4fab8d9be |
		|              | 1c864d19-fb3b-4497-bc07-416d98438732 |
		|              | 5a758ba8-f24b-4d44-92a5-214487325e73 |
		|              | 3aa8c250-13ab-45a2-bf8f-8bb9895d5611 |
		|              | f33a870c-1723-4620-b961-6bf919b83219 |
		|              | fdf310a4-97d4-4fa7-839c-7f85d41c2eb4 |
		|              | f92211c7-2a92-4372-bfdd-2c98857c3809 |
		|              | b81eae80-560c-44fd-a330-7f3ef94e0a9f |
		|              | 683c125a-3b98-40ab-b667-a11ad4b17919 |
		|              | ed30f797-2bc6-412c-a0c7-01a79f33c753 |
		|              | 44f6c2bc-9e2c-4794-9751-67480cabed62 |
		|              | ec48dff0-7598-4bcc-bce4-afd916438b58 |
		|              | be1ed4a6-676e-4b0b-ab41-f14270b2a19d |
		|              | e0ab7d11-b550-42a7-a2a0-9d5bb76df796 |
		|              | 31a31d04-64e6-4dec-8482-c3d24c9c7565 |
		|              | 745f0997-c675-4bc9-be95-31953c5798e7 |
		|              | a1e3b3e7-de49-43ff-89a7-932a8c13ccbc |
		|              | 72a8ac10-bc57-4057-8907-a0dbda260f5c |
		|              | 6024bf9d-246a-4d54-ad0e-94ff38ec22bd |
		|              | ea82c1ae-aa28-4a14-b5c9-e5aa7dd96100 |
		|              | 9895b991-9933-40be-93bb-5f77b89ee5ba |
		|              | f3332d77-a051-40ba-a883-f0fc134713ce |
		|              | 2a81fe81-18f3-4d11-bab0-8c0a746e279a |
		|              | f9c11de5-9e6e-4b73-9986-ab067a7bc0ab |
		|              | 66f880f2-9ee1-4f42-a8fe-3d1f3589351e |
		|              | de4925bc-b320-42b5-909a-f545450ef16b |
		|              | d53204a5-cd76-45f2-8bcb-a6a078a788cc |
		|              | ca229fec-a815-417c-b680-8dfa0bc7fff3 |
		|              | 2ac24e50-4203-4c23-b510-94dea87d42e8 |
		|              | 3018962a-6355-4dbb-aa99-e141e4763a51 |
		|              | a6747aa9-0c9f-484b-8014-27d95e5c1afd |
		|              | 80441694-000f-4e73-8c93-5916a9a3c86d |
		|              | ebb3445a-5273-4062-87ae-ecc415060e84 |
		|              | 25643e61-1bd2-46cf-9ee6-85214cde47bc |
		|              | 67e6521a-42e4-4226-a3c7-5033e4861a54 |
		|              | 2f011352-0a2f-4ce6-9a16-7014a1037bd3 |
		|              | f224bbde-85f8-4981-9908-d01e8fb62787 |
		|              | 0debb6c2-f189-40c9-8420-b43147a7bdb3 |
		|              | 4cd7f32e-16cf-41a0-b766-648f6f686eed |
		|              | 67974b2c-7068-4920-a8de-632b7f428148 |
		|              | c5a3b0a2-fc1e-4a7c-8be8-fea57cb5d71d |
		|              | 27b4bbf3-7aef-4e62-bc55-8d9c954cfc65 |
		|              | 15d349b5-584c-4bfe-9650-42e9476a0175 |
		|              | 56a9b23b-87ba-4853-91d7-b01c95637fdd |
		|              | 3068f1c5-15fc-46ca-b688-61f8c7910551 |
		|              | d574bee3-d494-4733-b2f4-febcaa69dbe2 |
		|              | e1bf7483-9e52-44e3-aba7-0c72e9e20a5c |
		|              | d9889c0e-498a-42af-b89d-01518040f7e9 |
		|              | 734604c6-c68c-4b16-8d12-4f1071f79ecb |
		|              | c4eca2d2-0596-4e37-839a-2315773f3514 |
		|              | 70f45ad0-7119-4d02-9acd-238779df6735 |
		|              | c4bcbc74-1750-4654-888e-9bff582bb06b |
		|              | 7b2c84a6-62f3-4f82-a1d1-b235b24b6e94 |
		|              | 47fbb66a-ab86-4edf-a109-280a5ba6aacb |
		|              | 2886704a-8534-4740-802d-831498acfc5e |
		|              | a159261d-cc3b-49d8-9609-da5eb164a9bf |
		|              | 857d0cbd-1ba4-4593-9a93-1b3ad58f97d4 |
		|              | 091cba13-2258-48b2-a85b-4ae55be28c29 |
		|              | 58c66937-8093-4168-bf4f-eccb2e3aacbf |
		|              | d544c44e-b6b5-431e-ae4a-5c8ed92dae65 |
		|              | 7995d48f-935f-4190-bf37-8102a0538f2e |
		|              | fff084e0-24ee-47e6-9b3c-eddca35f2e24 |
		|              | 2fa7ba90-686a-4f46-9078-e913e1eb7e7a |
		|              | d15867ca-0e79-457d-a964-d1f935e3ab7c |
		|              | 14a82c4d-802d-4f67-926f-81de92cab917 |
		|              | 28d1745f-f977-4a3b-8e96-6a97d782c78c |
		|              | 39378dbe-c5e5-4882-9a46-a4f8606c5d8e |
		|              | cf051bc2-16e2-465d-a71f-a49118209762 |
		|              | f1148e18-d82b-4ef6-9320-b29104a89e4d |
		|              | 3d1dbda3-ee02-45bd-8dc5-d76b1fa666cc |
		|              | 6727fea3-d6ca-4e2d-8618-b209a919eeda |
		|              | 32e5bf02-1e9f-4b98-a06a-e4f49c531d23 |
		|              | d851dc56-9e36-4afc-8551-179055e1995c |
		|              | 93d68f09-a0c2-42fe-baf9-0276433e8de7 |
		|              | 94a3b3e4-c24a-438e-a6c4-23935da28bf3 |
		|              | 626cc08a-25a0-4f4f-a373-6b97afbf419c |
		|              | 5e5d1249-e8da-44e7-8aa7-a96024e93c2d |
		|              | 95e94909-df15-446e-bbb5-2a796353d29b |
		|              | 01fb7c9d-ec74-4f43-b404-c918f7fe7fe5 |
		|              | be43e79d-e36d-49f9-8fd8-8f2c7bc3d0bd |
		|              | ecf19c2a-6373-429c-a8de-2f4a7322e84f |
		|              | a1f45664-cd06-4572-a580-f5afd1996b89 |
		|              | 2d5d37f5-faea-48b4-b033-a60a2544b031 |
		|              | d7c65add-6884-45c8-8353-c3857f98b3cf |
		|              | 34836ed7-3953-4c3f-8f56-1d410c716fdf |
		|              | 18a9f732-944a-4d23-b713-43be830ea9b6 |
		|              | 0b6a65dc-8449-4643-bf30-07690dbeee1c |
		|              | 99ca3111-a417-4bbf-aa9e-006c697375bf |
		|              | ca034370-fb4c-4073-8b05-97fe7b5f0857 |
		|              | d20f9bed-4032-491c-9300-aeb41915dcf1 |
		|              | 1ead4132-fbb5-4bc9-8506-33b08e10e587 |
		|              | 5b59fdd2-60ab-4127-822b-fa81c3962f8d |
		|              | f99102a3-5024-48db-b9e4-db1fc9a69043 |
		|              | 7ba82116-0fcc-4e7d-9f88-ae21744d2651 |
		|              | 62156712-8463-4630-a76f-13b461711ab9 |
		|              | 7d68e00b-b7c2-477f-b4fe-5b8cff4e229a |
		|              | deb8cc38-c170-4567-8f14-20b5dcad8922 |
		|              | 90d9f82d-0524-4db8-9cfa-f14cabceda02 |
		|              | b7b2ff6f-a347-4853-b051-fc128913776c |
		|              | 3f437067-7ef1-481c-a631-db5d33031241 |
		|              | 5d9983ac-8748-4780-95d9-16f877c299da |
		|              | 9c6540c4-5d9a-4655-a24d-560ead889e68 |
		|              | 45550274-958a-4042-be9d-775a996946d4 |
		|              | 17df4975-f739-4574-8f06-6aafd3a28ad8 |
		|              | 0fe3fa89-8361-46d9-b584-e415dfc5675a |
		|              | fadc2ef0-417c-4cde-a8d7-04cad69647b1 |
		|              | de1bd71b-29e0-4f48-b4d2-71384e92b9f5 |
		|              | 61fd3afc-d74a-450b-a6c3-0858260112df |
		|              | 25d5b084-4274-4edb-ae49-699197eb6eee |
		|              | c1575687-5bb1-4604-a524-dcbed3a3e3f0 |
		|              | 5a628ba1-94ac-440e-b78d-296f5f2b5da7 |
		|              | c8204022-f046-4606-ad8d-d9683bfb25a6 |
		|              | d89bf663-2ed8-44d2-935c-b0cf9a9a84f2 |
		|              | 83a4a011-ac69-4213-95e3-395aa6a6bfe0 |
		|              | 8643d35e-e326-4628-9e52-96908c53e2d9 |
		|              | b09f67a1-343f-4359-ae43-7e324858381d |
		|              | e87eae67-5495-451e-a531-9abebd2f23e0 |
		|              | ac4d0e5d-1afe-410f-a65b-4913ba85ce92 |
		|              | 6d5a0965-470e-4794-a0ac-4e1d80a18736 |
		|              | e5105ca6-3ef2-443e-830a-b915cb8a42dd |
		|              | c6ab4f61-cd95-4596-ac74-47ce66ff997b |
		|              | bb72366d-e7ab-40f9-9fc6-f62ec41c9cc9 |
	And the following distribution periods exist
		| DistributionPeriodId | Value |
		| AS-1920              | 1200  |
		| AS-2021              | 2000  |
	And the following profiles exist
		| DistributionPeriodId | Type          | TypeValue | Year | Occurrence | ProfiledValue |
		| AS-1920              | CalendarMonth | October   | 1920 | 1          | 1200          |
		| AS-2021              | CalendarMonth | April     | 2021 | 1          | 2000          |
	And the following profile pattern exists
		| FundingStreamId | FundingPeriodId |
		| 1619            | AS-2021         |
	When funding is refreshed
	Then the following published provider ids are upserted
		| PublishedProviderId                                           | Status  |
		| publishedprovider-1000000-<FundingPeriodId>-<FundingStreamId> | Updated |
		| publishedprovider-1000002-<FundingPeriodId>-<FundingStreamId> | Draft   |

	Examples:
		| FundingStreamId | FundingPeriodId | FundingPeriodName               | TemplateVersion | ProviderVersionId  |
		| 1619            | AS-2021         | Academies Academic Year 2020-21 | 1.2             | 1619-providers-1.0 |