Imports System

Public Class APTProformadatasetDataset

    Public Shared DatasetDefinitionName As String = "APT Proforma dataset"

    Public Property Id As String

    Public Property BudgetId As String

    Public Property ProviderUrn As String

    Public Property ProviderName As String

    Public Property DatasetName As String

    Public Property BasicEntitlementPrimaryAmountPerPupil As Decimal

    Public Property BasicEntitlementPrimaryPupilUnits As Decimal

    Public Property BasicEntitlementPrimarySubtotal As Decimal

    Public Property BasicEntitlementPrimaryProportion As Decimal

    Public Property BasicEntitlementPrimaryNotionalSEN As Decimal

    Public Property BasicEntitlementKS3AmountPerPupil As Decimal

    Public Property BasicEntitlementKS3PupilUnits As Decimal

    Public Property BasicEntitlementKS3Subtotal As Decimal

    Public Property BasicEntitlementKS3Proportion As Decimal

    Public Property BasicEntitlementKS3NotionalSEN As Decimal

    Public Property BasicEntitlementKS4AmountPerPupil As Decimal

    Public Property BasicEntitlementKS4PupilUnits As Decimal

    Public Property BasicEntitlementKS4Subtotal As Decimal

    Public Property BasicEntitlementKS4Proportion As Decimal

    Public Property BasicEntitlementKS4NotionalSEN As Decimal

    Public Property BasicEntitlementTotal As Decimal

    Public Property FSMPrimaryFSMFSM6 As Decimal

    Public Property FSMPrimaryAmountPerPupil As Decimal

    Public Property FSMPrimaryNumberonRoll As Decimal

    Public Property FSMPrimarySubtotal As Decimal

    Public Property FSMPrimaryNotionalSEN As Decimal

    Public Property FSMSecondaryFSMFSM6 As Decimal

    Public Property FSMSecondaryAmountPerPupil As Decimal

    Public Property FSMSecondaryNumberOnRoll As Decimal

    Public Property FSMSecondarySubtotal As Decimal

    Public Property FSMSecondaryNotionalSEN As Decimal

    Public Property IDACIPrimaryBFAmountPerPupil As Decimal

    Public Property IDACISecondaryBFAmountPerPupil As Decimal

    Public Property IDACIPrimaryBFNumberOnRoll As Decimal

    Public Property IDACISecondaryBFNumberonRoll As Decimal

    Public Property IDACIBFSubtotal As Decimal

    Public Property IDACIBFPrimaryNotionalSEN As Decimal

    Public Property IDACIBFSecondaryNotionalSEN As Decimal

    Public Property IDACIPrimaryBEAmountPerPupil As Decimal

    Public Property IDACISecondaryBEAmountperPupil As Decimal

    Public Property IDACIPrimaryBENumberonRoll As Decimal

    Public Property IDACISecondaryBENumberonRoll As Decimal

    Public Property IDACIBESubtotal As Decimal

    Public Property IDACIBEPrimaryNotionalSEN As Decimal

    Public Property IDACIBESecondaryNotionalSEN As Decimal

    Public Property IDACIPrimaryBDAmountPerPupil As Decimal

    Public Property IDACISecondaryBDAmountperPupil As Decimal

    Public Property IDACIPrimaryBDNumberonRoll As Decimal

    Public Property IDACISecondaryBDNumberonRoll As Decimal

    Public Property IDACIBDSubtotal As Decimal

    Public Property IDACIBDPrimaryNotionalSEN As Decimal

    Public Property IDACIBDSecondaryNotionalSEN As Decimal

    Public Property IDACIPrimaryBCAmountPerPupil As Decimal

    Public Property IDACISecondaryBCAmountperPupil As Decimal

    Public Property IDACIPrimaryBCNumberonRoll As Decimal

    Public Property IDACISecondaryBCNumberonRoll As Decimal

    Public Property IDACIBCSubtotal As Decimal

    Public Property IDACIBCPrimaryNotionalSEN As Decimal

    Public Property IDACIBCSecondaryNotionalSEN As Decimal

    Public Property IDACIPrimaryBBAmountPerPupil As Decimal

    Public Property IDACISecondaryBBAmountperPupil As Decimal

    Public Property IDACIPrimaryBBNumberonRoll As Decimal

    Public Property IDACISecondaryBBNumberonRoll As Decimal

    Public Property IDACIBBSubtotal As Decimal

    Public Property IDACIBBPrimaryNotionalSEN As Decimal

    Public Property IDACIBBSecondaryNotionalSEN As Decimal

    Public Property IDACIPrimaryBAAmountPerPupil As Decimal

    Public Property IDACISecondaryBAAmountperPupil As Decimal

    Public Property IDACIPrimaryBANumberonRoll As Decimal

    Public Property IDACISecondaryBANumberonRoll As Decimal

    Public Property IDACIBASubtotal As Decimal

    Public Property IDACIBAPrimaryNotionalSEN As Decimal

    Public Property IDACIBASecondaryNotionalSEN As Decimal

    Public Property DeprivationTotal As Decimal

    Public Property DeprivationProportion As Decimal

    Public Property LookedAfterChildrenDescripton As Decimal

    Public Property LookedAfterChildrenAmountPerPupil As Decimal

    Public Property LookedAfterChildrenNumberOnRoll As Decimal

    Public Property LookedAfterChildrenSubtotal As Decimal

    Public Property LookedAfterChildrenProportion As Decimal

    Public Property LookedAfterChildrenNotionalSEN As Decimal

    Public Property EALPrimary123NA As Decimal

    Public Property EALPrimaryAmountPerPupil As Decimal

    Public Property EALPrimaryNumberOnRoll As Decimal

    Public Property EALPrimarySubtotal As Decimal

    Public Property EALPrimaryNotionalSEN As Decimal

    Public Property EALSecondary123NA As Decimal

    Public Property EALSecondaryAmountPerPupil As Decimal

    Public Property EALSecondaryNumberOnRoll As Decimal

    Public Property EALSecondarySubtotal As Decimal

    Public Property EALSecondaryNotionalSEN As Decimal

    Public Property EALProportion As Decimal

    Public Property MobilityPrimaryAmountPerPupil As Decimal

    Public Property MobilitySecondaryAmountPerPupil As Decimal

    Public Property MobilityPrimaryNumberOnRoll As Decimal

    Public Property MobilitySecondaryNumberOnRoll As Decimal

    Public Property MobilitySubtotal As Decimal

    Public Property MobilityProportion As Decimal

    Public Property MobilityPrimaryNotionalSEN As Decimal

    Public Property MobilitySecondaryNotionalSEN As Decimal

    Public Property PriorAttainmentPrimary7378NA As Decimal

    Public Property PriorAttainmentPrimarynewEFSPWeighting As Decimal

    Public Property PriorAttainmentPrimaryAmountPerPupil As Decimal

    Public Property PriorAttainmentofeligibleY14 As Decimal

    Public Property PriorAttainmentofeligibleY56 As Decimal

    Public Property PriorAttainmenteligibleofNumberOnRollPrimary As Decimal

    Public Property PriorAttainmentPrimarySubtotal As Decimal

    Public Property PriorAttainmentPrimaryNotionalSEN As Decimal

    Public Property PriorAttainmentSecondaryAmountPerPupil As Decimal

    Public Property PriorAttainmentSecondaryNumberOnRoll As Decimal

    Public Property PriorAttainmentSecondarySubtotal As Decimal

    Public Property PriorAttainmentTotal As Decimal

    Public Property PriorAttainmentProportion As Decimal

    Public Property PriorAttainmentSecondaryNotionalSEN As Decimal

    Public Property PrimaryLumpSum As Decimal

    Public Property SecondaryLumpSum As Decimal

    Public Property TotalLumpSum As Decimal

    Public Property LumpSumProportion As Decimal

    Public Property PrimaryLumpSumNotionalSEN As Decimal

    Public Property SecondaryLumpSumNotionalSEN As Decimal

    Public Property SparsityPrimaryLumpSum As Decimal

    Public Property SparsitySecondaryLumpSum As Decimal

    Public Property SparsityMiddleSchoolLumpSum As Decimal

    Public Property SparsityAllThroughLumpSum As Decimal

    Public Property SparsityLumpSumTotal As Decimal

    Public Property SparsityProportion As Decimal

    Public Property PrimarySparsityNotionalSEN As Decimal

    Public Property SecondarySparsityNotionalSEN As Decimal

    Public Property Fixedortaperedsparsityprimarylumpsum As Decimal

    Public Property Fixedortaperedsparsitysecondarylumpsum As Decimal

    Public Property Fixedortaperedsparsitymiddleschoollumpsum As Decimal

    Public Property Fixedortaperedsparsityallthroughlumpsum As Decimal

    Public Property PrimaryDistanceThreshold As Decimal

    Public Property SecondaryDistanceThreshold As Decimal

    Public Property MiddleSchoolDistanceThreshold As Decimal

    Public Property AllThroughDistanceThreshold As Decimal

    Public Property Primarypupilnumberaverageyeargroupthreshold As Decimal

    Public Property Secondarypupilnumberaverageyeargroupthreshold As Decimal

    Public Property MiddleSchoolpupilnumberaverageyeargroupthreshold As Decimal

    Public Property AllThroughpupilnumberaverageyeargroupthreshold As Decimal

    Public Property FringePaymentsTotal As Decimal

    Public Property FringePaymentsProportion As Decimal

    Public Property SplitSitesTotal As Decimal

    Public Property SplitSitesProportion As Decimal

    Public Property SplitSitesNotionalSEN As Decimal

    Public Property RatesTotal As Decimal

    Public Property RatesProportion As Decimal

    Public Property RatesNotionalSEN As Decimal

    Public Property PFIFundingTotal As Decimal

    Public Property PFIFundingProportion As Decimal

    Public Property PFINotionalSEN As Decimal

    Public Property AdditionallumpsumforschoolsamalgamatedduringFY1516 As Decimal

    Public Property ExceptionalCircumstance1Total As Decimal

    Public Property ExceptionalCircumstance1Proportion As Decimal

    Public Property ExceptionalCircumstance1PrimaryNotionalSEN As Decimal

    Public Property ExceptionalCircumstance1SecondaryNotionalSEN As Decimal

    Public Property Additionalsparsitylumpsumforsmallschools As Decimal

    Public Property ExceptionalCircumstance2Total As Decimal

    Public Property ExceptionalCircumstance2Proportion As Decimal

    Public Property ExceptionalCircumstance2NotionalSEN As Decimal

    Public Property ExceptionalCircumstance3 As Decimal

    Public Property ExceptionalCircumstance3Total As Decimal

    Public Property ExceptionalCircumstance3Proportion As Decimal

    Public Property ExceptionalCircumstance3NotionalSEN As Decimal

    Public Property ExceptionalCircumstance4 As Decimal

    Public Property ExceptionalCircumstance4Total As Decimal

    Public Property ExceptionalCircumstance4Proportion As Decimal

    Public Property ExceptionalCircumstance4NotionalSEN As Decimal

    Public Property ExceptionalCircumstance5 As Decimal

    Public Property ExceptionalCircumstance5Total As Decimal

    Public Property ExceptionalCircumstance5Proportion As Decimal

    Public Property ExceptionalCircumstance5NotionalSEN As Decimal

    Public Property ExceptionalCircumstance6 As Decimal

    Public Property ExceptionalCircumstance6Total As Decimal

    Public Property ExceptionalCircumstance6Proportion As Decimal

    Public Property ExceptionalCircumstance6NotionalSEN As Decimal

    Public Property TotalFundingforSchoolsBlockFormulaExcludingMFGFundingTotal As Decimal

    Public Property TotalFundingforSchoolsBlockFormulaExcludingMFGProportion As Decimal

    Public Property TotalFundingforSchoolsBlockFormulaExcludingMFGNotionalSEN As Decimal

    Public Property MinimumFundingGuarantee As Decimal

    Public Property ApplyingCappingandScalingFactors As Decimal

    Public Property CappingFactor As Decimal

    Public Property ScalingFactor As Decimal

    Public Property Totaldeductionifcappingandscalingfactorsareapplied As Decimal

    Public Property MFGNetTotalFundingTotal As Decimal

    Public Property MFGNetTotalFundingProportion As Decimal

    Public Property HighNeedsThreshold As Decimal

    Public Property Additionalfundingfromthehighneedsbudget As Decimal

    Public Property GrowthFund As Decimal

    Public Property FundingRollsFund As Decimal

    Public Property TotalFundingforSchoolsBlockFormula As Decimal

    Public Property DistributedthroughBasicEntitlement As Decimal

    Public Property PupilLedFunding As Decimal

    Public Property PrimaryRatio As Decimal

    Public Property SecondaryRatio As Decimal

    Public Property ReceptionUpliftYesNo As Decimal

    Public Property ReceptionUpliftPupilUnits As Decimal

    Public Property PriorAttainmentofeligibleY7 As Decimal

    Public Property PriorAttainmentofeligibleY811 As Decimal

    Public Property Secondarylowattainmentyear7weighting As Decimal
End Class
