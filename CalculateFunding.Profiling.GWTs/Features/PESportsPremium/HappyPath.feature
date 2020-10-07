Feature: HappyPath

# IdentifierName is literal 'UKPRN', 'UPIN' or '?'(=blank)
# Identifier is value of the UKPRN or UPIN or '?'(=blank)
# Allocation Start and End Dates are in dd/mm/yyyy format or '?'(=blank)

@ADPIntegrationTest
Scenario: A981 Integer allocation value without rounding
	Given an ADP request exists for OrgId 'ORG0017981' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue |
	| AY1819             | 22284.00        |

	When  the request is processed

	Then  an ADP Allocation Profile response is created for OrgId 'ORG0017981' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue | 
	| AY1819             | 22284.00        |

	And   an ADP Delivery Profile response is created which contains the following
	| Period  | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
	| October | 1          | 2018       | CalendarMonth | 12999.00     | AY1819             |
	| April   | 1          | 2019       | CalendarMonth | 9285.00      | AY1819             |

	# Success message
	And   the service returns HTTP status code '200'
	  
@ADPIntegrationTest
Scenario: A982 Integer allocation value with rounding
	Given an ADP request exists for OrgId 'ORG0017982' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue |
	| AY1819             | 7514.00         |

	When  the request is processed

	Then  an ADP Allocation Profile response is created for OrgId 'ORG0017982' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue | 
	| AY1819             | 7514.00         |

	And   an ADP Delivery Profile response is created which contains the following
	| Period  | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
	| October | 1          | 2018       | CalendarMonth | 4383.00      | AY1819             |
	| April   | 1          | 2019       | CalendarMonth | 3131.00      | AY1819             |
	
	# Success message
	And   the service returns HTTP status code '200'
	
@ADPIntegrationTest
Scenario: A983 Non-integer allocation value 
	Given an ADP request exists for OrgId 'ORG0017983' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue |
	| AY1819             | 15217.50        |

	When  the request is processed

	Then  an ADP Allocation Profile response is created for OrgId 'ORG0017983' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue | 
	| AY1819             | 15217.50        |

	And   an ADP Delivery Profile response is created which contains the following
	| Period  | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
	| October | 1          | 2018       | CalendarMonth | 8877.00      | AY1819             |
	| April   | 1          | 2019       | CalendarMonth | 6340.50      | AY1819             |

	# Success message
	And   the service returns HTTP status code '200'

@ADPIntegrationTest
Scenario: A984 Zero allocation value 
	Given an ADP request exists for OrgId 'ORG0017984' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue |
	| AY1819             | 0.00            |

	When  the request is processed

	Then  an ADP Allocation Profile response is created for OrgId 'ORG0017984' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue | 
	| AY1819             | 0.00            |

	And   an ADP Delivery Profile response is created which contains the following
	| Period  | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
	| October | 1          | 2018       | CalendarMonth | 0.00         | AY1819             |
	| April   | 1          | 2019       | CalendarMonth | 0.00         | AY1819             |

	# Success message
	And   the service returns HTTP status code '200'

@ADPIntegrationTest
Scenario: A985 Negative integer allocation value 
	Given an ADP request exists for OrgId 'ORG0017985' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue |
	| AY1819             | -15725.00       |

	When  the request is processed

	Then  an ADP Allocation Profile response is created for OrgId 'ORG0017985' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue | 
	| AY1819             | -15725.00       |

	And   an ADP Delivery Profile response is created which contains the following
	| Period  | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
	| October | 1          | 2018       | CalendarMonth | -9173.00     | AY1819             |
	| April   | 1          | 2019       | CalendarMonth | -6552.00     | AY1819             |

	# Success message
	And   the service returns HTTP status code '200'

@ADPIntegrationTest
Scenario: A986 Sub-pound allocation value 
	Given an ADP request exists for OrgId 'ORG0017986' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue |
	| AY1819             | 0.75            |

	When  the request is processed

	Then  an ADP Allocation Profile response is created for OrgId 'ORG0017986' IdentifierName '?' Identifier '?' AllocationStartDate '?' AllocationEndDate '?' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue | 
	| AY1819             | 0.75            |

	And   an ADP Delivery Profile response is created which contains the following
	| Period | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
	| October    | 1          | 2018       | CalendarMonth | 0.00         | AY1819             |
	| April    | 1          | 2019       | CalendarMonth | 0.75        | AY1819             |

	# Success message
	And   the service returns HTTP status code '200'

@ADPIntegrationTest
Scenario: A987 Integer allocation value with full-year 2-payment allocation dates and UKPRN
	Given an ADP request exists for OrgId 'ORG0017987' IdentifierName 'UKPRN' Identifier '10017987' AllocationStartDate '01/09/2018' AllocationEndDate '31/08/2019' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue |
	| AY1819             | 9468.00         |

	When  the request is processed

	Then  an ADP Allocation Profile response is created for OrgId 'ORG0017987' IdentifierName 'UKPRN' Identifier '10017987' AllocationStartDate '01/09/2018' AllocationEndDate '31/08/2019' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue | 
	| AY1819             | 9468.00         |

	And   an ADP Delivery Profile response is created which contains the following
	| Period | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
	| October    | 1          | 2018       | CalendarMonth | 5523.00      | AY1819             |
	| April    | 1          | 2019       | CalendarMonth | 3945.00      | AY1819             |

	# Success message
	And   the service returns HTTP status code '200'

@ADPIntegrationTest
Scenario: A988 Integer allocation value with part-year 2-payment allocation dates and rounding
	Given an ADP request exists for OrgId 'ORG0017988' IdentifierName '?' Identifier '?' AllocationStartDate '01/01/2019' AllocationEndDate '15/07/2019' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue |
	| AY1819             | 10000.00        |

	When  the request is processed

	Then  an ADP Allocation Profile response is created for OrgId 'ORG0017988' IdentifierName '?' Identifier '?' AllocationStartDate '01/01/2019' AllocationEndDate '15/07/2019' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue | 
	| AY1819             | 10000.00        |

	And   an ADP Delivery Profile response is created which contains the following
	| Period  | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
	| October | 1          | 2018       | CalendarMonth | 5833.00      | AY1819             |
	| April   | 1          | 2019       | CalendarMonth | 4167.00      | AY1819             |

	# Success message
	And   the service returns HTTP status code '200'

@ADPIntegrationTest
Scenario: A989 Non-integer allocation value with part-year 1-payment allocation dates
	Given an ADP request exists for OrgId 'ORG0017989' IdentifierName '?' Identifier '?' AllocationStartDate '01/05/2019' AllocationEndDate '31/08/2019' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue |
	| AY1819             | 16707.20        |

	When  the request is processed

	Then  an ADP Allocation Profile response is created for OrgId 'ORG0017989' IdentifierName '?' Identifier '?' AllocationStartDate '01/05/2019' AllocationEndDate '31/08/2019' and FSP 'PSG1819' as follows
	| DistributionPeriod | AllocationValue | 
	| AY1819             | 16707.20        |

	And   an ADP Delivery Profile response is created which contains the following
	| Period | Occurrence | PeriodYear | PeriodType    | ProfileValue | DistributionPeriod |
	| April  | 1          | 2019       | CalendarMonth | 16707.20     | AY1819             |

	# Success message
	And   the service returns HTTP status code '200'
