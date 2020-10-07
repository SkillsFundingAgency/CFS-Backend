Feature: AdultSkills1920

@ADPIntegrationTest
Scenario: A999 AAC1920 Normal Profile
Given an ADP request exists for OrgId 'ORG0017981' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2019' AllocationEndDate '01/07/2020' and FSP 'AAC16191920' as follows
| DistributionPeriod | AllocationValue	|
| FY1920			 | 13626.00			|
| FY2021			 | 8874.00			|

When  the request is processed

Then  an ADP Allocation Profile response is created for OrgId 'ORG0017981' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2019' AllocationEndDate '01/07/2020' and FSP 'AAC16191920' as follows
| DistributionPeriod | AllocationValue	| 
| FY1920             | 13626.00			|
| FY2021             | 8874.00          |

And   an ADP Delivery Profile response is created which contains the following
| Period    | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
| September | 1          | 2019       | CalendarMonth | 2680.00      | FY1920             |
| October   | 1          | 2019       | CalendarMonth | 2714.00      | FY1920             |
| November  | 1          | 2019       | CalendarMonth | 2112.00      | FY1920             |
| December  | 1          | 2019       | CalendarMonth | 1590.00      | FY1920             |
| January   | 1          | 2020       | CalendarMonth | 1590.00      | FY1920             |
| February  | 1          | 2020       | CalendarMonth | 1476.00      | FY1920             |
| March     | 1          | 2020       | CalendarMonth | 1464.00      | FY1920             |
| April     | 1          | 2020       | CalendarMonth | 2826.00      | FY2021             |
| May       | 1          | 2020       | CalendarMonth | 2610.00      | FY2021             |
| June      | 1          | 2020       | CalendarMonth | 2160.00      | FY2021             |
| July      | 1          | 2020       | CalendarMonth | 1278.00      | FY2021             |

# Success message
And   the service returns HTTP status code '200'

@ADPIntegrationTest
Scenario: AAC1829 - Periods 2-12
    Given an ADP request exists for OrgId 'ORG0018007' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2019' AllocationEndDate '01/07/2020' and FSP 'AAC16191920' as follows
    | DistributionPeriod | AllocationValue	|
    | FY1920			 | 24573.00			|
	| FY2021			 | 16003.00			|

    When  the request is processed

    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018007' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2019' AllocationEndDate '01/07/2020' and FSP 'AAC16191920' as follows
    | DistributionPeriod | AllocationValue	|
    | FY1920			 | 24573.00			|
	| FY2021			 | 16003.00			|

    And   an ADP Delivery Profile response is created which contains the following
    | Period    | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
    | September | 1          | 2019       | CalendarMonth | 4833.00      | FY1920             |
    | October   | 1          | 2019       | CalendarMonth | 4894.00      | FY1920             |
    | November  | 1          | 2019       | CalendarMonth | 3809.00      | FY1920             |
    | December  | 1          | 2019       | CalendarMonth | 2867.00      | FY1920             |
    | January   | 1          | 2020       | CalendarMonth | 2867.00      | FY1920             |
    | February  | 1          | 2020       | CalendarMonth | 2662.00      | FY1920             |
    | March     | 1          | 2020       | CalendarMonth | 2641.00      | FY1920             |
    | April     | 1          | 2020       | CalendarMonth | 5096.00      | FY2021             |
    | May       | 1          | 2020       | CalendarMonth | 4707.00      | FY2021             |
    | June      | 1          | 2020       | CalendarMonth | 3895.00      | FY2021             |
    | July      | 1          | 2020       | CalendarMonth | 2305.00      | FY2021             |

       # Success message
       And   the service returns HTTP status code '200'

	   # This Funding Stream does not exist in CFS
@ignore ADPIntegrationTest
Scenario: 16-18APPS1920 - Short Allocation Months across FY
    Given an ADP request exists for OrgId 'ORG0018001' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2019' AllocationEndDate '01/07/2020' and FSP '16-18APPS1920' as follows
    | DistributionPeriod | AllocationValue	|
    | FY1920             | 71681.00			|
	| FY2021             | 36191.00			|

    When  the request is processed

    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018001' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2019' AllocationEndDate '01/07/2020' and FSP '16-18APPS1920' as follows
    | DistributionPeriod | AllocationValue	|
    | FY1920             | 71681.00			|
	| FY2021             | 36191.00			|

    And   an ADP Delivery Profile response is created which contains the following
    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
    | September    | 1          | 2019       | CalendarMonth | 10231.00		| FY1920             |
    | October    | 1          | 2019       | CalendarMonth | 10231.00		| FY1920             |
    | November    | 1          | 2019       | CalendarMonth | 10231.00		| FY1920             |
    | December    | 1          | 2019       | CalendarMonth | 10231.00		| FY1920             |
    | January    | 1          | 2020       | CalendarMonth | 10231.00		| FY1920             |
    | February    | 1          | 2020       | CalendarMonth | 10231.00		| FY1920             |
    | March    | 1          | 2020       | CalendarMonth | 10295.00		| FY1920             |
    | April    | 1          | 2020       | CalendarMonth | 9061.00		| FY2021             |
    | May    | 1          | 2020       | CalendarMonth | 9061.00		| FY2021             |
    | June    | 1          | 2020       | CalendarMonth | 9061.00		| FY2021             |
    | July    | 1          | 2020       | CalendarMonth | 9008.00		| FY2021             |

       # Success message
       And   the service returns HTTP status code '200'

# This Funding Stream does not exist in CFS
@ignore ADPIntegrationTest	   
Scenario: 16-18APPS1920 - Short Allocation Different Months across FY 
    Given an ADP request exists for OrgId 'ORG0018002' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2019' AllocationEndDate '01/07/2020' and FSP '16-18APPS1920' as follows
    | DistributionPeriod | AllocationValue	|
	| FY1920             | 52418.00			|
	| FY2021             | 26465.00			|
	

    When  the request is processed

    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018002' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2019' AllocationEndDate '01/07/2020' and FSP '16-18APPS1920' as follows
    | DistributionPeriod | AllocationValue	|
    | FY1920             | 52418.00			|
	| FY2021             | 26465.00			|

    And   an ADP Delivery Profile response is created which contains the following
    | Period    | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
    | September | 1          | 2019       | CalendarMonth | 7482.00      | FY1920             |
    | October   | 1          | 2019       | CalendarMonth | 7482.00      | FY1920             |
    | November  | 1          | 2019       | CalendarMonth | 7482.00      | FY1920             |
    | December  | 1          | 2019       | CalendarMonth | 7482.00      | FY1920             |
    | January   | 1          | 2020       | CalendarMonth | 7482.00      | FY1920             |
    | February  | 1          | 2020       | CalendarMonth | 7482.00      | FY1920             |
    | March     | 1          | 2020       | CalendarMonth | 7526.00      | FY1920             |
    | April     | 1          | 2020       | CalendarMonth | 6626.00      | FY2021             |
    | May       | 1          | 2020       | CalendarMonth | 6626.00      | FY2021             |
    | June      | 1          | 2020       | CalendarMonth | 6626.00      | FY2021             |
    | July      | 1          | 2020       | CalendarMonth | 6587.00      | FY2021             |

       # Success message
       And   the service returns HTTP status code '200'

# This Funding Stream does not exist in CFS
@ignore
Scenario: 16-18APPS1920 - Periods 9-12 one FY
    Given an ADP request exists for OrgId 'ORG0018003' IdentifierName '?' Identifier '?' AllocationStartDate '01/04/2020' AllocationEndDate '01/07/2020' and FSP '16-18APPS1920' as follows
    | DistributionPeriod | AllocationValue	|
	| FY2021             | 31646.00			|

    When  the request is processed

    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018003' IdentifierName '?' Identifier '?' AllocationStartDate '01/04/2020' AllocationEndDate '01/07/2020' and FSP '16-18APPS1920' as follows
    | DistributionPeriod | AllocationValue	|
    | FY2021             | 31646.00			|

    And   an ADP Delivery Profile response is created which contains the following
    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
    | April  | 1          | 2020       | CalendarMonth | 7923.00      | FY2021             |
    | May    | 1          | 2020       | CalendarMonth | 7923.00      | FY2021             |
    | June   | 1          | 2020       | CalendarMonth | 7923.00      | FY2021             |
    | July   | 1          | 2020       | CalendarMonth | 7877.00      | FY2021             |

       # Success message
       And   the service returns HTTP status code '200'
#
#Scenario: 16-18NLA1718 - Periods 1-5
#    Given an ADP request exists for OrgId 'ORG0018004' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/12/2017' and FSP '16-18NLA1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 138373.00		|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018004' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/12/2017' and FSP '16-18NLA1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 138373.00		|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Aug    | 1          | 2017       | CalendarMonth | 11044.00		| AY1819             |
#    | Sep    | 1          | 2017       | CalendarMonth | 18892.00		| AY1819             |
#    | Oct    | 1          | 2017       | CalendarMonth | 24675.00		| AY1819             |
#    | Nov    | 1          | 2017       | CalendarMonth | 29311.00		| AY1819             |
#    | Dec    | 1          | 2017       | CalendarMonth | 54451.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: 16-18NLA1718 - Periods 1-5
#    Given an ADP request exists for OrgId 'ORG0018005' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/12/2017' and FSP '16-18NLA1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 2857.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018005' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/12/2017' and FSP '16-18NLA1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 2857.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Aug    | 1          | 2017       | CalendarMonth | 228.00			| AY1819             |
#    | Sep    | 1          | 2017       | CalendarMonth | 390.00			| AY1819             |
#    | Oct    | 1          | 2017       | CalendarMonth | 509.00			| AY1819             |
#    | Nov    | 1          | 2017       | CalendarMonth | 605.00			| AY1819             |
#    | Dec    | 1          | 2017       | CalendarMonth | 1125.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AATO1617 - Periods 1-8
#    Given an ADP request exists for OrgId 'ORG0018008' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2016' AllocationEndDate '01/03/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 136000.00		|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018008' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2016' AllocationEndDate '01/03/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 136000.00		|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Aug    | 1          | 2016       | CalendarMonth | 16009.00		| AY1819             |
#    | Sep    | 1          | 2016       | CalendarMonth | 16850.00		| AY1819             |
#    | Oct    | 1          | 2016       | CalendarMonth | 17565.00		| AY1819             |
#    | Nov    | 1          | 2016       | CalendarMonth | 17145.00		| AY1819             |
#    | Dec    | 1          | 2016       | CalendarMonth | 15925.00		| AY1819             |
#    | Jan    | 1          | 2017       | CalendarMonth | 17124.00		| AY1819             |
#    | Feb    | 1          | 2017       | CalendarMonth | 17124.00		| AY1819             |
#    | Mar    | 1          | 2017       | CalendarMonth | 18258.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AATO1617 - Periods 2-12
#    Given an ADP request exists for OrgId 'ORG0018009' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2016' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 384970.00		|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018009' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2016' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 384970.00		|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Sep    | 1          | 2016       | CalendarMonth | 34950.00		| AY1819             |
#    | Oct    | 1          | 2016       | CalendarMonth | 36434.00		| AY1819             |
#    | Nov    | 1          | 2016       | CalendarMonth | 35561.00		| AY1819             |
#    | Dec    | 1          | 2016       | CalendarMonth | 33030.00		| AY1819             |
#    | Jan    | 1          | 2017       | CalendarMonth | 35517.00		| AY1819             |
#    | Feb    | 1          | 2017       | CalendarMonth | 35517.00		| AY1819             |
#    | Mar    | 1          | 2017       | CalendarMonth | 37874.00		| AY1819             |
#    | Apr    | 1          | 2017       | CalendarMonth | 32491.00		| AY1819             |
#    | May    | 1          | 2017       | CalendarMonth | 31991.00		| AY1819             |
#    | Jun    | 1          | 2017       | CalendarMonth | 33916.00		| AY1819             |
#    | Jul    | 1          | 2017       | CalendarMonth | 37689.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AATO1617 - Periods 2-12
#    Given an ADP request exists for OrgId 'ORG0018010' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2016' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 88053.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018010' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2016' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 88053.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Sep    | 1          | 2016       | CalendarMonth | 7994.00		| AY1819             |
#    | Oct    | 1          | 2016       | CalendarMonth | 8333.00		| AY1819             |
#    | Nov    | 1          | 2016       | CalendarMonth | 8134.00		| AY1819             |
#    | Dec    | 1          | 2016       | CalendarMonth | 7555.00		| AY1819             |
#    | Jan    | 1          | 2017       | CalendarMonth | 8124.00		| AY1819             |
#    | Feb    | 1          | 2017       | CalendarMonth | 8124.00		| AY1819             |
#    | Mar    | 1          | 2017       | CalendarMonth | 8662.00		| AY1819             |
#    | Apr    | 1          | 2017       | CalendarMonth | 7432.00		| AY1819             |
#    | May    | 1          | 2017       | CalendarMonth | 7317.00		| AY1819             |
#    | Jun    | 1          | 2017       | CalendarMonth | 7758.00		| AY1819             |
#    | Jul    | 1          | 2017       | CalendarMonth | 8620.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AATO1617 - Periods 5-12
#    Given an ADP request exists for OrgId 'ORG0018011' IdentifierName '?' Identifier '?' AllocationStartDate '01/12/2016' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 24355.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018011' IdentifierName '?' Identifier '?' AllocationStartDate '01/12/2016' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue |
#    | AY1819             | 24355.00 |
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Dec    | 1          | 2016       | CalendarMonth | 9667.00		| AY1819             |
#    | Jan    | 1          | 2017       | CalendarMonth | 1983.00		| AY1819             |
#    | Feb    | 1          | 2017       | CalendarMonth | 1983.00		| AY1819             |
#    | Mar    | 1          | 2017       | CalendarMonth | 2113.00		| AY1819             |
#    | Apr    | 1          | 2017       | CalendarMonth | 2055.00		| AY1819             |
#    | May    | 1          | 2017       | CalendarMonth | 2024.00		| AY1819             |
#    | Jun    | 1          | 2017       | CalendarMonth | 2146.00		| AY1819             |
#    | Jul    | 1          | 2017       | CalendarMonth | 2384.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AATO1617 - Periods 5-12
#    Given an ADP request exists for OrgId 'ORG0018012' IdentifierName '?' Identifier '?' AllocationStartDate '01/12/2016' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 249750.00		|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018012' IdentifierName '?' Identifier '?' AllocationStartDate '01/12/2016' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 249750.00		|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Dec    | 1          | 2016       | CalendarMonth | 99126.00		| AY1819             |
#    | Jan    | 1          | 2017       | CalendarMonth | 20330.00		| AY1819             |
#    | Feb    | 1          | 2017       | CalendarMonth | 20330.00		| AY1819             |
#    | Mar    | 1          | 2017       | CalendarMonth | 21677.00		| AY1819             |
#    | Apr    | 1          | 2017       | CalendarMonth | 21079.00		| AY1819             |
#    | May    | 1          | 2017       | CalendarMonth | 20754.00		| AY1819             |
#    | Jun    | 1          | 2017       | CalendarMonth | 22003.00		| AY1819             |
#    | Jul    | 1          | 2017       | CalendarMonth | 24451.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AATO1617 - Periods 9-12
#    Given an ADP request exists for OrgId 'ORG0018013' IdentifierName '?' Identifier '?' AllocationStartDate '01/04/2017' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 14639.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018013' IdentifierName '?' Identifier '?' AllocationStartDate '01/04/2017' AllocationEndDate '01/07/2017' and FSP 'AATO1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 14639.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | May    | 1          | 2017       | CalendarMonth | 3441.00		| AY1819             |
#    | Jun    | 1          | 2017       | CalendarMonth | 3648.00		| AY1819             |
#    | Jul    | 1          | 2017       | CalendarMonth | 4055.00		| AY1819             |
#    | Apr    | 1          | 2017       | CalendarMonth | 3495.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AEBTO-LS1718 - Periods 1-3
#    Given an ADP request exists for OrgId 'ORG0018014' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/10/2017' and FSP 'AEBTO-LS1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 1875.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018014' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/10/2017' and FSP 'AEBTO-LS1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 1875.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Aug    | 1          | 2017       | CalendarMonth | 561.00			| AY1819             |
#    | Sep    | 1          | 2017       | CalendarMonth | 646.00			| AY1819             |
#    | Oct    | 1          | 2017       | CalendarMonth | 668.00			| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AEBTO-LS1718 - Periods 1-3
#    Given an ADP request exists for OrgId 'ORG0018015' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/10/2017' and FSP 'AEBTO-LS1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 7796.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018015' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/10/2017' and FSP 'AEBTO-LS1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 7796.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Aug    | 1          | 2017       | CalendarMonth | 2332.00		| AY1819             |
#    | Sep    | 1          | 2017       | CalendarMonth | 2688.00		| AY1819             |
#    | Oct    | 1          | 2017       | CalendarMonth | 2776.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AEBTO-TOL1718 - Periods 1-3
#    Given an ADP request exists for OrgId 'ORG0018016' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/10/2017' and FSP 'AEBTO-TOL1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 44331.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018016' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/10/2017' and FSP 'AEBTO-TOL1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 44331.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Sep    | 1          | 2017       | CalendarMonth | 15284.00		| AY1819             |
#    | Oct    | 1          | 2017       | CalendarMonth | 15786.00		| AY1819             |
#    | Aug    | 1          | 2017       | CalendarMonth | 13261.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: AEBTO-TOL1718 - Periods 1-3
#    Given an ADP request exists for OrgId 'ORG0018017' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/10/2017' and FSP 'AEBTO-TOL1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 38937.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018017' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/10/2017' and FSP 'AEBTO-TOL1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 38937.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Aug    | 1          | 2017       | CalendarMonth | 11647.00		| AY1819             |
#    | Sep    | 1          | 2017       | CalendarMonth | 13425.00		| AY1819             |
#    | Oct    | 1          | 2017       | CalendarMonth | 13865.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: ALLF1617 - Periods 9-12
#    Given an ADP request exists for OrgId 'ORG0018018' IdentifierName '?' Identifier '?' AllocationStartDate '01/04/2017' AllocationEndDate '01/07/2017' and FSP 'ALLF1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 50000.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018018' IdentifierName '?' Identifier '?' AllocationStartDate '01/04/2017' AllocationEndDate '01/07/2017' and FSP 'ALLF1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 50000.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Sep    | 1          | 2016       | CalendarMonth | 0.00			| AY1819             |
#    | Oct    | 1          | 2016       | CalendarMonth | 0.00			| AY1819             |
#    | Nov    | 1          | 2016       | CalendarMonth | 0.00			| AY1819             |
#    | Dec    | 1          | 2016       | CalendarMonth | 0.00			| AY1819             |
#    | Jan    | 1          | 2017       | CalendarMonth | 0.00			| AY1819             |
#    | Feb    | 1          | 2017       | CalendarMonth | 0.00			| AY1819             |
#    | Mar    | 1          | 2017       | CalendarMonth | 0.00			| AY1819             |
#    | Apr    | 1          | 2017       | CalendarMonth | 12612.00		| AY1819             |
#    | May    | 1          | 2017       | CalendarMonth | 12463.00		| AY1819             |
#    | Jun    | 1          | 2017       | CalendarMonth | 12517.00		| AY1819             |
#    | Jul    | 1          | 2017       | CalendarMonth | 12408.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: ALLF1617 - Periods 9-12
#    Given an ADP request exists for OrgId 'ORG0018019' IdentifierName '?' Identifier '?' AllocationStartDate '01/04/2017' AllocationEndDate '01/07/2017' and FSP 'ALLF1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 36830.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018019' IdentifierName '?' Identifier '?' AllocationStartDate '01/04/2017' AllocationEndDate '01/07/2017' and FSP 'ALLF1617' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 36830.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Sep    | 1          | 2016       | CalendarMonth | 0.00			| AY1819             |
#    | Oct    | 1          | 2016       | CalendarMonth | 0.00			| AY1819             |
#    | Nov    | 1          | 2016       | CalendarMonth | 0.00			| AY1819             |
#    | Dec    | 1          | 2016       | CalendarMonth | 0.00			| AY1819             |
#    | Jan    | 1          | 2017       | CalendarMonth | 0.00			| AY1819             |
#    | Feb    | 1          | 2017       | CalendarMonth | 0.00			| AY1819             |
#    | Mar    | 1          | 2017       | CalendarMonth | 0.00			| AY1819             |
#    | Apr    | 1          | 2017       | CalendarMonth | 9290.00		| AY1819             |
#    | May    | 1          | 2017       | CalendarMonth | 9180.00		| AY1819             |
#    | Jun    | 1          | 2017       | CalendarMonth | 9220.00		| AY1819             |
#    | Jul    | 1          | 2017       | CalendarMonth | 9140.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: ANLA1718 - Periods 1-5
#    Given an ADP request exists for OrgId 'ORG0018020' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/12/2017' and FSP 'ANLA1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 46493.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018020' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/12/2017' and FSP 'ANLA1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 46493.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue	| DistributionPeriod |
#    | Aug    | 1          | 2017       | CalendarMonth | 6369.00		| AY1819             |
#    | Sep    | 1          | 2017       | CalendarMonth | 8359.00		| AY1819             |
#    | Oct    | 1          | 2017       | CalendarMonth | 9694.00		| AY1819             |
#    | Nov    | 1          | 2017       | CalendarMonth | 10664.00		| AY1819             |
#    | Dec    | 1          | 2017       | CalendarMonth | 11407.00		| AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
#
#Scenario: ANLA1718 - Periods 1-5
#    Given an ADP request exists for OrgId 'ORG0018021' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/12/2017' and FSP 'ANLA1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 25260.00			|
#
#    When  the request is processed
#
#    Then  an ADP Allocation Profile response is created for OrgId 'ORG0018021' IdentifierName '?' Identifier '?' AllocationStartDate '01/08/2017' AllocationEndDate '01/12/2017' and FSP 'ANLA1718' as follows
#    | DistributionPeriod | AllocationValue	|
#    | AY1819             | 25260.00			|
#
#    And   an ADP Delivery Profile response is created which contains the following
#    | Period | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
#    | Aug    | 1          | 2017       | CalendarMonth | 3460.00 | AY1819             |
#    | Sep    | 1          | 2017       | CalendarMonth | 4541.00 | AY1819             |
#    | Oct    | 1          | 2017       | CalendarMonth | 5267.00 | AY1819             |
#    | Nov    | 1          | 2017       | CalendarMonth | 5794.00 | AY1819             |
#    | Dec    | 1          | 2017       | CalendarMonth | 6198.00 | AY1819             |
#
#       # Success message
#       And   the service returns HTTP status code '200'
