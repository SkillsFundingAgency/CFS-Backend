Feature: ValidateTransactionData

# IdentifierName is literal 'UKPRN', 'UPIN' or '?'(=blank)
# Identifier is value of the UKPRN or UPIN or '?'(=blank)
# Allocation Start and End Dates are in dd/mm/yyyy format or '?'(=blank)

@ADPIntegrationTest
Scenario: A991 Fail: Unrecognised FSP
    Given an ADP request exists for OrgId '?' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PESPORTPREM11819' as follows
    | DistributionPeriod | AllocationValue |
    | AY1819             | 10000.00        |

    When  the request is processed

	# Error 404
	Then  the service returns HTTP status code '404'

@ADPIntegrationTest
Scenario: A992 Fail: Unrecognised DistributionPeriod
    Given an ADP request exists for OrgId '?' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
    | DistributionPeriod | AllocationValue |
    | FY1819             | 10000.00        |

    When  the request is processed

	# Error 400
	Then  the service returns HTTP status code '400'

@ADPIntegrationTest
Scenario: A993 Fail: Dates within 1819 but EndDate before StartDate
    Given an ADP request exists for OrgId '?' IdentifierName '?' Identifier '?' AllocationStartDate '01/06/2019' AllocationEndDate '20/12/2018' and FSP 'PSG1819' as follows
    | DistributionPeriod | AllocationValue |
    | AY1819             | 10000.00        |

    When  the request is processed

	# This is not currently treated as an error. Instead the reponse contains a valid Allocation Profile with an empty Delivery Profile.
    Then  an ADP Allocation Profile response is created for OrgId '?' IdentifierName '?' Identifier '?' AllocationStartDate '01/06/2019' AllocationEndDate '20/12/2018' and FSP 'PSG1819' as follows
    | DistributionPeriod | AllocationValue | 
    | AY1819             | 10000.00        |

	# Success message
	Then  the service returns HTTP status code '200'

@ADPIntegrationTest
Scenario: A994 Fail: Valid date format but StartDate out of bounds
    Given an ADP request exists for OrgId '?' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2015' AllocationEndDate '?' and FSP 'PSG1819' as follows
    | DistributionPeriod | AllocationValue |
    | AY1819             | 25000.00        |

    When  the request is processed

	# This is not currently treated as an error. 
    Then  an ADP Allocation Profile response is created for OrgId '?' IdentifierName '?' Identifier '?' AllocationStartDate '01/09/2015' AllocationEndDate '?' and FSP 'PSG1819' as follows
    | DistributionPeriod | AllocationValue | 
    | AY1819             | 25000.00        |

    And   an ADP Delivery Profile response is created which contains the following
    | Period  | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
    | October | 1          | 2018       | CalendarMonth | 14583.00     | AY1819             |
    | April   | 1          | 2019       | CalendarMonth | 10417.00     | AY1819             |
	
	# Success message
	Then  the service returns HTTP status code '200'

@ADPIntegrationTest
Scenario: A995 Fail: Valid date format but EndDate out of bounds
    Given an ADP request exists for OrgId '?' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '31/08/2022' and FSP 'PSG1819' as follows
    | DistributionPeriod | AllocationValue |
    | AY1819             | 72000.00        |

    When  the request is processed

	# This is not currently treated as an error.
    Then  an ADP Allocation Profile response is created for OrgId '?' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '31/08/2022' and FSP 'PSG1819' as follows
    | DistributionPeriod | AllocationValue | 
    | AY1819             | 72000.00        |

    And   an ADP Delivery Profile response is created which contains the following
    | Period  | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
    | October | 1          | 2018       | CalendarMonth | 42000.00     | AY1819             |
    | April   | 1          | 2019       | CalendarMonth | 30000.00     | AY1819             |
	
	# Success message
	Then  the service returns HTTP status code '200'
