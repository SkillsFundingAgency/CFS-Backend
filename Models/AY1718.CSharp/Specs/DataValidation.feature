Feature: P004_PriRate


@mytag
Scenario: Only Primary providers should have Primary Rate

	Given 'Phase' in 'APT Provider Information' is 'Primary'
	And 'PrimaryNOR' in 'APT Provider Information' is greater than 0
	Then 'P004_PriRate' should be greater than 0

Scenario: Only Primary providers should have Primary Rate
	Given 'Phase' in 'APT Provider Information' is not 'Primary'
	Then 'P004_PriRate' should be  0

Scenario: Primary Rate should be greater than 2000

	Given 'Phase' in 'APT Provider Information' is 'Primary'
	And 'PrimaryNOR' in 'APT Provider Information' is greater than 0
	Then 'P004_PriRate' should be greater than or equal to 2000

Scenario: New Openers should not use NOR
	Given 'OpeningDate' in 'APT Provider Information' is '1/4/2018' or later
	Then 'P004_PriRate' should be equal to 'Primary Value' in 'APT Provider Information' 

Scenario: New Openers should not use NOR
	Given 'OpeningDate' in 'APT Provider Information' is before '1/4/2018'
	Then 'P004_PriRate' should be the product of:
	| Dataset                  | Field       |
	| APT Provider Information | PrimaryNOR  |
	| APT Provider Information | PrimaryRate |