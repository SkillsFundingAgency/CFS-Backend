Feature: Basic Per Pupil Entitlement
	In order to avoid silly mistakes
	As a math idiot
	I want to be told the sum of two numbers

Background: 
	Given I am using the 'SBS1718' model
	And I have the following global variables:
	| NOR_Pri_SBS | NOR_Pri_KS4_SBS |
	| 2780.00     | 24              |

@mytag
Scenario: Basic Entitlement Primary Rates
	Given I have the following 'APT Provider Information' provider dataset:
	| URN      | Date Opened | Local Authority |
	| 10027549 | 12/12/1980  | Northumberland  |

	And I have the following 'APT Basic Entitlement' provider dataset:
	| URN      |  Primary Amount Per Pupil | Primary Amount | Primary Notional SEN |
	| 10027549 |  2807.00                  | 0.00           | 0.00                 |

	And I have the following 'APT Local Authority' provider dataset:
	| Local Authority | Phase   | Local Authority |
	| Northumberland  | Primary| 1243            |

	And I have the following 'Census Weights' provider dataset:
	| URN      | Phase | Local Authority |
	| 10027549 | 2807.00    | 1243            |

	And I have the following 'Census Number Counts' provider dataset:
	| URN      | NOR  | NOR Primary | NOR Y1ToY2 | NOR Y3ToY6 |
	| 10027549 | 2807 | 1243        |            |            |

	When I calculate the allocations for the provider

	Then the allocation statement should be:
	| UKPRN    | P004_PriRate | P005_PriBESubtotal | P006_NSEN_PriBE | P006a_NSEN_PriBE_Percent |
	| 10027549 | 2807.00      | 3489101.00         | 0.00            | 0.00                     |


