Imports System

Public Class Calculations
    Inherits CalculationBase

    Public Property CensusPupilCharacteristics As CensusPupilCharacteristicsDataset

    Public Property LocalAuthorityAverages As LocalAuthorityAveragesDataset

    Public Property APTProformadataset As APTProformadatasetDataset

    Public Property APTLocalfactorsdataset As APTLocalfactorsdatasetDataset

    Public Property APTAdjustedFactorsdataset As APTAdjustedFactorsdatasetDataset

    Public Property APTFinalBaselinesdataset As APTFinalBaselinesdatasetDataset

    Public Property APTNewISBdataset As APTNewISBdatasetDataset

    Public Property APTRecoupmentdataset As APTRecoupmentdatasetDataset

    Public Property APTInputsandAdjustments As APTInputsandAdjustmentsDataset

    Public Property _1617_CTCs_Data As _1617_CTCs_DataDataset

    Public Property _1617_CTCs_Constants As _1617_CTCs_ConstantsDataset

    Public Property _1617_BasketLAs As _1617_BasketLAsDataset

    Public Property Recoupment As RecoupmentDataset

    Public Property RecoupmentNewISB As RecoupmentNewISBDataset

    <Calculation(Id:="64a431fdc66f43499ff0f822b2d46d01")>
    <CalculationSpecification(Id:="P004_PriRate", Name:="P004_PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P004_PriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="145fbb684a1e447ca7d9ef0d6b600805")>
    <CalculationSpecification(Id:="P005_PriBESubtotal", Name:="P005_PriBESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P005_PriBESubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a49c1011ec1848d082cbea01a5276b60")>
    <CalculationSpecification(Id:="P006_NSEN_PriBE", Name:="P006_NSEN_PriBE")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P006_NSEN_PriBE As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="08d6fdf3af4a45b69c0485f641daebdf")>
    <CalculationSpecification(Id:="P006a_NSEN_PriBE_Percent", Name:="P006a_NSEN_PriBE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P006a_NSEN_PriBE_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="acb2b30fa0fe453da17a55cd0c352665")>
    <CalculationSpecification(Id:="P007_InYearPriBE_Subtotal", Name:="P007_InYearPriBE_Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P007_InYearPriBE_Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="8f11b2f5ada54a349094fbfc5fd07fe0")>
    <CalculationSpecification(Id:="P009_KS3Rate", Name:="P009_KS3Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P009_KS3Rate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f8a5290566474bf8b3981596500ed244")>
    <CalculationSpecification(Id:="P010_KS3_BESubtotal", Name:="P010_KS3_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P010_KS3_BESubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="e402ff1db75543bbb73c651f8214589a")>
    <CalculationSpecification(Id:="P011_NSEN_KS3BE_percent", Name:="P011_NSEN_KS3BE_percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P011_NSEN_KS3BE_percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b3b69fc6f0264e6299e22ff79a3dcd6c")>
    <CalculationSpecification(Id:="P011a_NSEN_KS3BE_Percent", Name:="P011a_NSEN_KS3BE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P011a_NSEN_KS3BE_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="678b817540a848f68ff3e16102307497")>
    <CalculationSpecification(Id:="P012_InYearKS3_BESubtotal", Name:="P012_InYearKS3_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P012_InYearKS3_BESubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="33779c3ff4b2456089ed2822b43842c3")>
    <CalculationSpecification(Id:="P014_KS4Rate", Name:="P014_KS4Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P014_KS4Rate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="129728806b3c4e13afeaa97fdfbeb0b0")>
    <CalculationSpecification(Id:="P015_KS4_BESubtotal", Name:="P015_KS4_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P015_KS4_BESubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2f57b656b8dd4849814810c69a5a8253")>
    <CalculationSpecification(Id:="P016_NSEN_KS4BE", Name:="P016_NSEN_KS4BE")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P016_NSEN_KS4BE As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f2605c7c8b8d4abb9b007f8c7e9759af")>
    <CalculationSpecification(Id:="P016a_NSEN_KS4BE_Percent", Name:="P016a_NSEN_KS4BE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P016a_NSEN_KS4BE_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="515a8e9e010042c3bd028fbf7ed910b1")>
    <CalculationSpecification(Id:="P018_InYearKS4_BESubtotal", Name:="P018_InYearKS4_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P018_InYearKS4_BESubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f5d4d6a5845a44c3a7adc213a35de917")>
    <CalculationSpecification(Id:="P297_DedelegationRetained", Name:="P297_DedelegationRetained")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="cc9eac91ede6480eb46f748efb3a9d11", Name:="Dedelegation Retained by LA")>
    Public Function P297_DedelegationRetained As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5ea1dbe9b74b4e3686a77ea2b4f28950")>
    <CalculationSpecification(Id:="P142_EAL1PriFactor", Name:="P142_EAL1PriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P142_EAL1PriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="53bc9ec0ad204143a6e0c5d5c42d7869")>
    <CalculationSpecification(Id:="P144_EAL1PriRate", Name:="P144_EAL1PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P144_EAL1PriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="16a64807b2ed431a883b10a9f4f5a4c5")>
    <CalculationSpecification(Id:="P145_EAL1PriSubtotal", Name:="P145_EAL1PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P145_EAL1PriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="3e79618cd34442c0b6831834a343a4d6")>
    <CalculationSpecification(Id:="P146_InYearEAL1PriSubtotal", Name:="P146_InYearEAL1PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P146_InYearEAL1PriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ae2f35037a9544a69ac4ebcc161751c0")>
    <CalculationSpecification(Id:="P147_EAL2PriFactor", Name:="P147_EAL2PriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P147_EAL2PriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="076500d074654f4a9fb6ebb951340432")>
    <CalculationSpecification(Id:="P149_EAL2PriRate", Name:="P149_EAL2PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P149_EAL2PriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="3295ca53be9b4c40b27403767c6d4328")>
    <CalculationSpecification(Id:="P150_EAL2PriSubtotal", Name:="P150_EAL2PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P150_EAL2PriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1183cfc856fb4a6b8cbb8b2bdd90ec0c")>
    <CalculationSpecification(Id:="P151_InYearEAL2PriSubtotal", Name:="P151_InYearEAL2PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P151_InYearEAL2PriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1625abbb35cd4736a5781beaa393e428")>
    <CalculationSpecification(Id:="P152_EAL3PriFactor", Name:="P152_EAL3PriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P152_EAL3PriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1e588fee24764fe285c0788d25970717")>
    <CalculationSpecification(Id:="P154_EAL3PriRate", Name:="P154_EAL3PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P154_EAL3PriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d5e6e4cb233f41fb84da7d7908703887")>
    <CalculationSpecification(Id:="P155_EAL3PriSubtotal", Name:="P155_EAL3PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P155_EAL3PriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="92b68d173b4b417eb9449b461f2948ed")>
    <CalculationSpecification(Id:="P156_NSENPriEAL", Name:="P156_NSENPriEAL")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P156_NSENPriEAL As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="fd4d4d4634a34a8bb9c186465c21774f")>
    <CalculationSpecification(Id:="P156a_NSENPriEAL_Percent", Name:="P156a_NSENPriEAL_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P156a_NSENPriEAL_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="fac38efa47c24311a51d3eb49f14be30")>
    <CalculationSpecification(Id:="P157_InYearEAL3PriSubtotal", Name:="P157_InYearEAL3PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P157_InYearEAL3PriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="62f0b82e59774ba98a8c815a1cf1e41a")>
    <CalculationSpecification(Id:="P158_EAL1SecFactor", Name:="P158_EAL1SecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P158_EAL1SecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="fbc678ea9a8f45beba98dfabc313fb36")>
    <CalculationSpecification(Id:="P160_EAL1SecRate", Name:="P160_EAL1SecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P160_EAL1SecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="3a3de8748d7d49c2aee6d6ef78c625b4")>
    <CalculationSpecification(Id:="P161_EAL1SecSubtotal", Name:="P161_EAL1SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P161_EAL1SecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="969b028656054eb18c38afa839ed9f2e")>
    <CalculationSpecification(Id:="P162_InYearEAL1SecSubtotal", Name:="P162_InYearEAL1SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P162_InYearEAL1SecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="3d9510149308483991394a5f6114e81b")>
    <CalculationSpecification(Id:="P163_EAL2SecFactor", Name:="P163_EAL2SecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P163_EAL2SecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d5c12792ab7c4103939c2b54ae418295")>
    <CalculationSpecification(Id:="P165_EAL2SecRate", Name:="P165_EAL2SecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P165_EAL2SecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a60f5dab26e944c5842914c26a786b35")>
    <CalculationSpecification(Id:="P166_EAL2SecSubtotal", Name:="P166_EAL2SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P166_EAL2SecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9a4328c2ab764a70af819303177120d6")>
    <CalculationSpecification(Id:="P167_InYearEAL2SecSubtotal", Name:="P167_InYearEAL2SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P167_InYearEAL2SecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="45abdb92813e4e1a9f9573d6ac42463d")>
    <CalculationSpecification(Id:="P168_EAL3SecFactor", Name:="P168_EAL3SecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P168_EAL3SecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="fbf4f020a1f14f1caf5cb2ad5a091266")>
    <CalculationSpecification(Id:="P170_EAL3SecRate", Name:="P170_EAL3SecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P170_EAL3SecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="56406663274f4fc7a8895f4f5c1a5630")>
    <CalculationSpecification(Id:="P171_EAL3SecSubtotal", Name:="P171_EAL3SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P171_EAL3SecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="95c887e4fb344ad993a04909f1cfc072")>
    <CalculationSpecification(Id:="P172_NSENSecEAL", Name:="P172_NSENSecEAL")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P172_NSENSecEAL As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ad0095c72e904b4b856f215cac4b25ae")>
    <CalculationSpecification(Id:="P172a_NSENSecEAL_Percent", Name:="P172a_NSENSecEAL_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P172a_NSENSecEAL_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b1904f61e6aa46fd9daf1fa146bbf4c3")>
    <CalculationSpecification(Id:="P173_InYearEAL3SecSubtotal", Name:="P173_InYearEAL3SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P173_InYearEAL3SecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="cc46bc0d9d274c9c84785a3ac5451f8c")>
    <CalculationSpecification(Id:="P019_PriFSMFactor", Name:="P019_PriFSMFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P019_PriFSMFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d3dac5a4e7ce464c9b65a9bea2fc2e61")>
    <CalculationSpecification(Id:="P021_PriFSMRate", Name:="P021_PriFSMRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P021_PriFSMRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="672db5d03e5043f7b977e307660e50ab")>
    <CalculationSpecification(Id:="P022_PriFSMSubtotal", Name:="P022_PriFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P022_PriFSMSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ab5715c372a94299b809c8952ece4af3")>
    <CalculationSpecification(Id:="P023_InYearPriFSMSubtotal", Name:="P023_InYearPriFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P023_InYearPriFSMSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="57904e48b69f4c44b2b32b99c893b36f")>
    <CalculationSpecification(Id:="P024_PriFSM6Factor", Name:="P024_PriFSM6Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P024_PriFSM6Factor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5743b6a4f48d4f2eb411ea0172d793a5")>
    <CalculationSpecification(Id:="P026_PriFSM6Rate", Name:="P026_PriFSM6Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P026_PriFSM6Rate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b2aa7b8fcfc64b61b6519eddd66f7974")>
    <CalculationSpecification(Id:="P027_PriFSM6Subtotal", Name:="P027_PriFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P027_PriFSM6Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b8bae3a67b224f728ef37b351246ffb1")>
    <CalculationSpecification(Id:="P028_NSENFSMPri", Name:="P028_NSENFSMPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P028_NSENFSMPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="7b80726d9f684445be7e2127d9db73f3")>
    <CalculationSpecification(Id:="P028a_NSENFSMPri_Percent", Name:="P028a_NSENFSMPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P028a_NSENFSMPri_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="13c4c1eccf80438cb0fd3decf9fcc246")>
    <CalculationSpecification(Id:="P029_InYearPriFSM6Subtotal", Name:="P029_InYearPriFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P029_InYearPriFSM6Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="432f80ccd02147b390c387e0d5c89bcc")>
    <CalculationSpecification(Id:="P030_SecFSMFactor", Name:="P030_SecFSMFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P030_SecFSMFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6ba79a15ef244b70aec425623e0dcffa")>
    <CalculationSpecification(Id:="P032_SecFSMRate", Name:="P032_SecFSMRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P032_SecFSMRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9f8f59b429524f988924aeed96c8edb0")>
    <CalculationSpecification(Id:="P033_SecFSMSubtotal", Name:="P033_SecFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P033_SecFSMSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="09cd08e2d4b347fc8101586f60da8d90")>
    <CalculationSpecification(Id:="P034_InYearSecFSMSubtotal", Name:="P034_InYearSecFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P034_InYearSecFSMSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="83c0ee49e81340b9bac5854cf0074d41")>
    <CalculationSpecification(Id:="P035_SecFSM6Factor", Name:="P035_SecFSM6Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P035_SecFSM6Factor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="7cfe6ff33f16490ea59ad5fbf4149de8")>
    <CalculationSpecification(Id:="P037_SecFSM6Rate", Name:="P037_SecFSM6Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P037_SecFSM6Rate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="34e2b446e174416189a79cddbeabce9d")>
    <CalculationSpecification(Id:="P038_SecFSM6Subtotal", Name:="P038_SecFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P038_SecFSM6Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="fa0117bf9a494782a252353946a366c4")>
    <CalculationSpecification(Id:="P039_NSENFSMSec", Name:="P039_NSENFSMSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P039_NSENFSMSec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a691cf11b32447a7a56691bc71f0b978")>
    <CalculationSpecification(Id:="P039a_NSENFSMSec_Percent", Name:="P039a_NSENFSMSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P039a_NSENFSMSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d514bb9d32e54b8691c478b94566d43e")>
    <CalculationSpecification(Id:="P040_InYearSecFSM6Subtotal", Name:="P040_InYearSecFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P040_InYearSecFSM6Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="44ddbff77af84d6681468681dcef28ac")>
    <CalculationSpecification(Id:="P041_IDACIFPriFactor", Name:="P041_IDACIFPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P041_IDACIFPriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1ab1ec5ad40e4d1d8fb655770c815702")>
    <CalculationSpecification(Id:="P043_IDACIFPriRate", Name:="P043_IDACIFPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P043_IDACIFPriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="51812d0f0c0d45df9cd021ae471c730d")>
    <CalculationSpecification(Id:="P044_IDACIFPriSubtotal", Name:="P044_IDACIFPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P044_IDACIFPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a530b84c433642f3adf9b97985b8b3a7")>
    <CalculationSpecification(Id:="P045_NSENIDACIFPri", Name:="P045_NSENIDACIFPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P045_NSENIDACIFPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1b8b89ea51ef425eb1f8b89195889480")>
    <CalculationSpecification(Id:="P045a_NSENIDACIFPri_Percent", Name:="P045a_NSENIDACIFPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P045a_NSENIDACIFPri_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b4f9b968a3e0475b86327e75468e013f")>
    <CalculationSpecification(Id:="P046_InYearIDACIFPriSubtotal", Name:="P046_InYearIDACIFPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P046_InYearIDACIFPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ea0ab316720240189686932988c4da21")>
    <CalculationSpecification(Id:="P047_IDACIEPriFactor", Name:="P047_IDACIEPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P047_IDACIEPriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="e5788b6d5a674800b4cca26c2dceb1d0")>
    <CalculationSpecification(Id:="P049_IDACIEPriRate", Name:="P049_IDACIEPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P049_IDACIEPriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="336e05c765c84a778663a77de7202a3f")>
    <CalculationSpecification(Id:="P050_IDACIEPriSubtotal", Name:="P050_IDACIEPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P050_IDACIEPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a17c35eda5e740dbb1b0e7a70ed28ed0")>
    <CalculationSpecification(Id:="P051_NSENIDACIEPri", Name:="P051_NSENIDACIEPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P051_NSENIDACIEPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4213f7002b344affb53b67d24c203af7")>
    <CalculationSpecification(Id:="P051a_NSENIDACIEPri_Percent", Name:="P051a_NSENIDACIEPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P051a_NSENIDACIEPri_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="0f97330c8d1346b09cd052b365633721")>
    <CalculationSpecification(Id:="P052_InYearIDACIEPriSubtotal", Name:="P052_InYearIDACIEPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P052_InYearIDACIEPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="472a9bf3182c4f1594e67d775d87e749")>
    <CalculationSpecification(Id:="P053_IDACIDPriFactor", Name:="P053_IDACIDPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P053_IDACIDPriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2fdfbee6500e4e25a6998e58dbe7a9e1")>
    <CalculationSpecification(Id:="P055_IDACIDPriRate", Name:="P055_IDACIDPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P055_IDACIDPriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="32b539e710c845f188013a580da5b163")>
    <CalculationSpecification(Id:="P056_IDACIDPriSubtotal", Name:="P056_IDACIDPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P056_IDACIDPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4321614f74cd41bd864910e8f2a982bb")>
    <CalculationSpecification(Id:="P057_NSENIDACIDPri", Name:="P057_NSENIDACIDPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P057_NSENIDACIDPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="00adf70437ac4489b21eb0b22730c365")>
    <CalculationSpecification(Id:="P057a_NSENIDACIDPri_Percent", Name:="P057a_NSENIDACIDPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P057a_NSENIDACIDPri_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5b5e16bd87e24366b920a48a51419557")>
    <CalculationSpecification(Id:="P058_InYearIDACIDPriSubtotal", Name:="P058_InYearIDACIDPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P058_InYearIDACIDPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="484eb64a33e6459490c63647165adbbb")>
    <CalculationSpecification(Id:="P059_IDACICPriFactor", Name:="P059_IDACICPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P059_IDACICPriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ec77a49d62ca410e8152a12611da08d2")>
    <CalculationSpecification(Id:="P061_IDACICPriRate", Name:="P061_IDACICPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P061_IDACICPriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="685aaa4257a04a3eb17d056959ec969c")>
    <CalculationSpecification(Id:="P062_IDACICPriSubtotal", Name:="P062_IDACICPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P062_IDACICPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2db5a37bced5470eb213b2267f38fa6f")>
    <CalculationSpecification(Id:="P063_NSENIDACICPri", Name:="P063_NSENIDACICPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P063_NSENIDACICPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="45e75fb1c8d0444b9eed065f576952be")>
    <CalculationSpecification(Id:="P063a_NSENIDACICPri_Percent", Name:="P063a_NSENIDACICPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P063a_NSENIDACICPri_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6b57edfb06244569a017c8382ac990f8")>
    <CalculationSpecification(Id:="P064_InYearIDACICPriSubtotal", Name:="P064_InYearIDACICPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P064_InYearIDACICPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="7574ea83930f443a8da4c6374c8a1255")>
    <CalculationSpecification(Id:="P065_IDACIBPriFactor", Name:="P065_IDACIBPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P065_IDACIBPriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="3c235b83ec6c4f8d99a961b91283202d")>
    <CalculationSpecification(Id:="P067_IDACIBPriRate", Name:="P067_IDACIBPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P067_IDACIBPriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="62a7dd5c68e74a4892a678e0bdbb33ec")>
    <CalculationSpecification(Id:="P068_IDACIBPriSubtotal", Name:="P068_IDACIBPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P068_IDACIBPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4fdd8ae852024685b758d36fac159446")>
    <CalculationSpecification(Id:="P069_NSENIDACIBPri", Name:="P069_NSENIDACIBPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P069_NSENIDACIBPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="c3add91d4d264cd29b3b10bdae92f48a")>
    <CalculationSpecification(Id:="P069a_NSENIDACIBPri_Percent", Name:="P069a_NSENIDACIBPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P069a_NSENIDACIBPri_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="71c945341350478d9ef690e51667f1af")>
    <CalculationSpecification(Id:="P070_InYearIDACIBPriSubtotal", Name:="P070_InYearIDACIBPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P070_InYearIDACIBPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="90781d7916574e9e81c5091010f25fa3")>
    <CalculationSpecification(Id:="P071_IDACIAPriFactor", Name:="P071_IDACIAPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P071_IDACIAPriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="30bb526ed5104ee38d7146f8fb290f1c")>
    <CalculationSpecification(Id:="P073_IDACIAPriRate", Name:="P073_IDACIAPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P073_IDACIAPriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ffa10571708549de87ed61522ef32844")>
    <CalculationSpecification(Id:="P074_IDACIAPriSubtotal", Name:="P074_IDACIAPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P074_IDACIAPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="773b5aa6ba8d4613a46083260e38507f")>
    <CalculationSpecification(Id:="P075_NSENIDACIAPri", Name:="P075_NSENIDACIAPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P075_NSENIDACIAPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="305d42bd380347d08715f529e6fb6431")>
    <CalculationSpecification(Id:="P075a_NSENIDACIAPri_Percent", Name:="P075a_NSENIDACIAPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P075a_NSENIDACIAPri_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a8f9bd5addfb4d74b7d0ceff30d5b3de")>
    <CalculationSpecification(Id:="P076_InYearIDACIAPriSubtotal", Name:="P076_InYearIDACIAPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P076_InYearIDACIAPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="3704cc2d8dc742bb80c02209755867e0")>
    <CalculationSpecification(Id:="P077_IDACIFSecFactor", Name:="P077_IDACIFSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P077_IDACIFSecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="e00968cc479a437ebaf7d27cfb8992a8")>
    <CalculationSpecification(Id:="P079_IDACIFSecRate", Name:="P079_IDACIFSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P079_IDACIFSecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="131572c7552a4805861bf5e04c2a9585")>
    <CalculationSpecification(Id:="P080_IDACIFSecSubtotal", Name:="P080_IDACIFSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P080_IDACIFSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="e96016dd44ec42dc8e383932067ac415")>
    <CalculationSpecification(Id:="P081_NSENIDACIFSec", Name:="P081_NSENIDACIFSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P081_NSENIDACIFSec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="3678071354f84af2a92ae08da4b9340e")>
    <CalculationSpecification(Id:="P081a_NSENIDACIFSec_Percent", Name:="P081a_NSENIDACIFSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P081a_NSENIDACIFSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="3390e8dda59442a6b5988d09ba5f8252")>
    <CalculationSpecification(Id:="P082_InYearIDACIFSecSubtotal", Name:="P082_InYearIDACIFSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P082_InYearIDACIFSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="07cacff54773457a93d9159a5d99a83a")>
    <CalculationSpecification(Id:="P083_IDACIESecFactor", Name:="P083_IDACIESecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P083_IDACIESecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="745c535567274f3997564692173a335a")>
    <CalculationSpecification(Id:="P085_IDACIESecRate", Name:="P085_IDACIESecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P085_IDACIESecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="540b34f027b842038b45764d003b4e60")>
    <CalculationSpecification(Id:="P086_IDACIESecSubtotal", Name:="P086_IDACIESecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P086_IDACIESecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d6af399c616e49dfbee2ce4e865f2b04")>
    <CalculationSpecification(Id:="P087_NSENIDACIESec", Name:="P087_NSENIDACIESec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P087_NSENIDACIESec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="cb08404be3eb41efbd033dedbcc60383")>
    <CalculationSpecification(Id:="P87a_NSENIDACIESec_Percent", Name:="P87a_NSENIDACIESec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P87a_NSENIDACIESec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="26902c0a2575481e973140a34c06b7eb")>
    <CalculationSpecification(Id:="P088_InYearIDACIESecSubtotal", Name:="P088_InYearIDACIESecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P088_InYearIDACIESecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="cde2f4570c674468b93de33a9c307201")>
    <CalculationSpecification(Id:="P089_IDACIDSecFactor", Name:="P089_IDACIDSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P089_IDACIDSecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="91a1c69560a84217973720b82029dcbc")>
    <CalculationSpecification(Id:="P091_IDACIDSecRate", Name:="P091_IDACIDSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P091_IDACIDSecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9b2f879cbd034a0e8a4e430b3fee8961")>
    <CalculationSpecification(Id:="P092_IDACIDSecSubtotal", Name:="P092_IDACIDSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P092_IDACIDSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="53cfcb2aeba84bf586b9aae2c9d3b235")>
    <CalculationSpecification(Id:="P093_NSENIDACIDSec", Name:="P093_NSENIDACIDSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P093_NSENIDACIDSec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="57bf5541e4ab4e80819387425f7603d2")>
    <CalculationSpecification(Id:="P093a_NSENIDACIDSec_Percent", Name:="P093a_NSENIDACIDSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P093a_NSENIDACIDSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="fb548a20eff1480b80340e54eb9136a4")>
    <CalculationSpecification(Id:="P094_InYearIDACIDSecSubtotal", Name:="P094_InYearIDACIDSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P094_InYearIDACIDSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="bbfe39cb0cf84f7fb8873754ce07eea7")>
    <CalculationSpecification(Id:="P095_IDACICSecFactor", Name:="P095_IDACICSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P095_IDACICSecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f1bdfef2ebf04308a03d1f78b707ab2c")>
    <CalculationSpecification(Id:="P097_IDACICSecRate", Name:="P097_IDACICSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P097_IDACICSecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="49111bfd558447e2bc11e7b5a7e471be")>
    <CalculationSpecification(Id:="P098_IDACICSecSubtotal", Name:="P098_IDACICSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P098_IDACICSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="e76b0f7dff644541913397147bb1fabc")>
    <CalculationSpecification(Id:="P099_NSENIDACICSec", Name:="P099_NSENIDACICSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P099_NSENIDACICSec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="621f00485c6e4de99fd9a8435ed35ede")>
    <CalculationSpecification(Id:="P099a_NSENIDACICSec_Percent", Name:="P099a_NSENIDACICSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P099a_NSENIDACICSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6707b070aa4d4041ae170cd5fc05b921")>
    <CalculationSpecification(Id:="P100_InYearIDACICSecSubtotal", Name:="P100_InYearIDACICSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P100_InYearIDACICSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="eef248d949014280aae1e475220afe0b")>
    <CalculationSpecification(Id:="P101_IDACIBSecFactor", Name:="P101_IDACIBSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P101_IDACIBSecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="28a38dd37fd447f2bab08977c5f3a848")>
    <CalculationSpecification(Id:="P103_IDACIBSecRate", Name:="P103_IDACIBSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P103_IDACIBSecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4afeb9237b96410499dc378c099fa90d")>
    <CalculationSpecification(Id:="P104_IDACIBSecSubtotal", Name:="P104_IDACIBSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P104_IDACIBSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5ea259915c364b4596223e2a9b53ce24")>
    <CalculationSpecification(Id:="P105_NSENIDACIBSec", Name:="P105_NSENIDACIBSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P105_NSENIDACIBSec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="977e4e311ad34960ba3b7bbc66ed45c5")>
    <CalculationSpecification(Id:="P105a_NSENIDACIBSec_Percent", Name:="P105a_NSENIDACIBSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P105a_NSENIDACIBSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4c478d6123dd46fb943921885e2759c7")>
    <CalculationSpecification(Id:="P106_InYearIDACIBSecSubtotal", Name:="P106_InYearIDACIBSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P106_InYearIDACIBSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="670958f115bd45a4a7cc03a6294f58b6")>
    <CalculationSpecification(Id:="P107_IDACIASecFactor", Name:="P107_IDACIASecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P107_IDACIASecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b1ed967fcbad4f118050bd28d50a2542")>
    <CalculationSpecification(Id:="P109_IDACIASecRate", Name:="P109_IDACIASecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P109_IDACIASecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d5c4ae9e48444163af25c3a6fd8d2481")>
    <CalculationSpecification(Id:="P110_IDACIASecSubtotal", Name:="P110_IDACIASecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P110_IDACIASecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d22cc5b64c9940249d1e0c24b6d03bc7")>
    <CalculationSpecification(Id:="P111_NSENIDACIASec", Name:="P111_NSENIDACIASec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P111_NSENIDACIASec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="487d9b9c93524022b8cb85fab9b62b38")>
    <CalculationSpecification(Id:="P111a_NSENIDACIASec_Percent", Name:="P111a_NSENIDACIASec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P111a_NSENIDACIASec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5d60da4c2b824497a202b98647bc3e14")>
    <CalculationSpecification(Id:="P112_InYearIDACIASecSubtotal", Name:="P112_InYearIDACIASecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P112_InYearIDACIASecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="97660509f4f743a6ad3a173a88d5fc80")>
    <CalculationSpecification(Id:="P114_LACFactor", Name:="P114_LACFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P114_LACFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1dc1c5ff5d26417c85bcfef0bba1ba99")>
    <CalculationSpecification(Id:="P116_LACRate", Name:="P116_LACRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P116_LACRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a5079656e0fd4beb828f79939f176945")>
    <CalculationSpecification(Id:="P117_LACSubtotal", Name:="P117_LACSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P117_LACSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9664ce9cc2f64697a92efccd79774e71")>
    <CalculationSpecification(Id:="P118_NSENLAC", Name:="P118_NSENLAC")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P118_NSENLAC As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b1ef45c1c52f4df6984240a37c937811")>
    <CalculationSpecification(Id:="P118a_NSENLAC_Percent", Name:="P118a_NSENLAC_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P118a_NSENLAC_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="89d254aaa64047b480ad66292a342d2c")>
    <CalculationSpecification(Id:="P119_InYearLACSubtotal", Name:="P119_InYearLACSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P119_InYearLACSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ebe8fac6a14b47ee99e956d53f979950")>
    <CalculationSpecification(Id:="P174_MobPriFactor", Name:="P174_MobPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P174_MobPriFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5062f0de4c834ac783e5ae5e3f738ec0")>
    <CalculationSpecification(Id:="P176_MobPriRate", Name:="P176_MobPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P176_MobPriRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="61c6119c964d477ab9535c0dbb3c14a7")>
    <CalculationSpecification(Id:="P177_MobPriSubtotal", Name:="P177_MobPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P177_MobPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="495525c8617646c497ab0ed4914953e1")>
    <CalculationSpecification(Id:="P178_NSENMobPri", Name:="P178_NSENMobPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P178_NSENMobPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="369308b46d70430485e5c19ea493065c")>
    <CalculationSpecification(Id:="P178a_NSENMobPri_Percent", Name:="P178a_NSENMobPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P178a_NSENMobPri_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d67bac5ad7bc4eb398a3ff1b4abd4be9")>
    <CalculationSpecification(Id:="P179_InYearMobPriSubtotal", Name:="P179_InYearMobPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P179_InYearMobPriSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="850d1e3e107a45699dd1ff455f426730")>
    <CalculationSpecification(Id:="P180_MobSecFactor", Name:="P180_MobSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P180_MobSecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6d2cd8d13a864623b563dbf673c447d1")>
    <CalculationSpecification(Id:="P182_MobSecRate", Name:="P182_MobSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P182_MobSecRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="15e597bf8c844eee842f0717b0d7bd4a")>
    <CalculationSpecification(Id:="P183_MobSecSubtotal", Name:="P183_MobSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P183_MobSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2fe23a7adeb94d5aa2d7747f61211a4a")>
    <CalculationSpecification(Id:="P184_NSENMobSec", Name:="P184_NSENMobSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P184_NSENMobSec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6d0783616ccd486dbcec92f8d4c1a0a4")>
    <CalculationSpecification(Id:="P184a_NSENMobSec_Percent", Name:="P184a_NSENMobSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P184a_NSENMobSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b6e1c7e6c99744f8841cc4bc873b4de6")>
    <CalculationSpecification(Id:="P185_InYearMobSecSubtotal", Name:="P185_InYearMobSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P185_InYearMobSecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1bf9cf6c0899466793d361f4e25dfc2c")>
    <CalculationSpecification(Id:="P239_PriLumpSumFactor", Name:="P239_PriLumpSumFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P239_PriLumpSumFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="0eba0f5234644ff980a88536274bbda3")>
    <CalculationSpecification(Id:="P240_PriLumpSumRate", Name:="P240_PriLumpSumRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P240_PriLumpSumRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d12292704e314992a760189400d2d2d9")>
    <CalculationSpecification(Id:="P241_Primary_Lump_Sum", Name:="P241_Primary_Lump_Sum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P241_Primary_Lump_Sum As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f89ec97e01ba43c0ad726b423cb359dd")>
    <CalculationSpecification(Id:="P242_InYearPriLumpSumSubtotal", Name:="P242_InYearPriLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P242_InYearPriLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="faa503f7a97248c7ae27f087007e6d1f")>
    <CalculationSpecification(Id:="P243_SecLumpSumFactor", Name:="P243_SecLumpSumFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P243_SecLumpSumFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1f7ae3ae28804a7db8cae61993a4412c")>
    <CalculationSpecification(Id:="P244_SecLumpSumRate", Name:="P244_SecLumpSumRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P244_SecLumpSumRate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="709d7dbaab1b4a0882d3ea09b068298d")>
    <CalculationSpecification(Id:="P245_Secondary_Lump_Sum", Name:="P245_Secondary_Lump_Sum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P245_Secondary_Lump_Sum As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2d44fd52e67c40e4baa3f4558217f0e9")>
    <CalculationSpecification(Id:="P246_In YearSecLumpSumSubtotal", Name:="P246_In YearSecLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P246_InYearSecLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b5d47d42634649cf9f34f7c4de67fe75")>
    <CalculationSpecification(Id:="P247_NSENLumpSumPri", Name:="P247_NSENLumpSumPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P247_NSENLumpSumPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="e922541aa0d74ef4a7616c347a85c330")>
    <CalculationSpecification(Id:="P247a_NSENLumpSumPri_Percent", Name:="P247a_NSENLumpSumPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P247a_NSENLumpSumPri_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="48b9233c64af4d58aec950c9c188f437")>
    <CalculationSpecification(Id:="P248_NSENLumpSumSec", Name:="P248_NSENLumpSumSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P248_NSENLumpSumSec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6f1812ccaddf4d3da26f5d0463afeb9e")>
    <CalculationSpecification(Id:="P248a_NSENLumpSumSec_Percent", Name:="P248a_NSENLumpSumSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P248a_NSENLumpSumSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="c12e270af6f34d87be5f60e9fc533b7e")>
    <CalculationSpecification(Id:="P252_PFISubtotal", Name:="P252_PFISubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P252_PFISubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b29d13a106be43f1b482ef8e6d91a2ba")>
    <CalculationSpecification(Id:="P253_NSENPFI", Name:="P253_NSENPFI")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P253_NSENPFI As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="569379718eb44a4f9b9efde251c62327")>
    <CalculationSpecification(Id:="P253a_NSENPFI_Percent", Name:="P253a_NSENPFI_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P253a_NSENPFI_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d06db588ff5c4c2f88f34312cb53adf0")>
    <CalculationSpecification(Id:="P254_InYearPFISubtotal", Name:="P254_InYearPFISubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P254_InYearPFISubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="11a2d4e099e646759fc08b79f73c51b4")>
    <CalculationSpecification(Id:="P255_FringeSubtotal", Name:="P255_FringeSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P255_FringeSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="11e19e4f4c4143dab1b6665c27428a7c")>
    <CalculationSpecification(Id:="P257_InYearFringeSubtotal", Name:="P257_InYearFringeSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P257_InYearFringeSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="78cfae7788564ca4b8930d439fa6198f")>
    <CalculationSpecification(Id:="P261_Ex1Subtotal", Name:="P261_Ex1Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P261_Ex1Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="c4b3c545998847e0a3e55547369f94d6")>
    <CalculationSpecification(Id:="P262_NSENEx1", Name:="P262_NSENEx1")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P262_NSENEx1 As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="611c23e3eaa546d2867259e446990f9f")>
    <CalculationSpecification(Id:="P262a_NSENEx1_Percent", Name:="P262a_NSENEx1_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P262a_NSENEx1_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="8ea528a193424d00a752620e328c36f4")>
    <CalculationSpecification(Id:="P264_InYearEx1Subtotal", Name:="P264_InYearEx1Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P264_InYearEx1Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d4307c3fda5c48b98de07db3e5f5be7b")>
    <CalculationSpecification(Id:="P265_Ex2Subtotal", Name:="P265_Ex2Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P265_Ex2Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="cfdefb10f1e042759089703b3f469740")>
    <CalculationSpecification(Id:="P266_NSENEx2", Name:="P266_NSENEx2")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P266_NSENEx2 As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="de25318deb9e4e33b5c82441c386b786")>
    <CalculationSpecification(Id:="P266a_NSENEx2_Percent", Name:="P266a_NSENEx2_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P266a_NSENEx2_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="13a58a5750144c478d041bc6d6bc8992")>
    <CalculationSpecification(Id:="P267_InYearEx2Subtotal", Name:="P267_InYearEx2Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P267_InYearEx2Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="c913b7c04ae843b6a669a473ec401b3f")>
    <CalculationSpecification(Id:="P269_Ex3Subtotal", Name:="P269_Ex3Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P269_Ex3Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="329b58fa13f94794a4a913d6d43843df")>
    <CalculationSpecification(Id:="P270_NSENEx3", Name:="P270_NSENEx3")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P270_NSENEx3 As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ee3cb3c755fe45dc9abe160f9dafe7b5")>
    <CalculationSpecification(Id:="P270a_NSENEx3_Percent", Name:="P270a_NSENEx3_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P270a_NSENEx3_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="979af16c58d74367a32d1297f27a1898")>
    <CalculationSpecification(Id:="P271_InYearEx3Subtotal", Name:="P271_InYearEx3Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P271_InYearEx3Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="710f2f27e170469a99276c9e1c1d9928")>
    <CalculationSpecification(Id:="P273_Ex4Subtotal", Name:="P273_Ex4Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P273_Ex4Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="14191fdc64574c40a1c4e0121e0bfc48")>
    <CalculationSpecification(Id:="P274_NSENEx4", Name:="P274_NSENEx4")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P274_NSENEx4 As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4867ee0cfc214fe9979665b88825bff4")>
    <CalculationSpecification(Id:="P274a_NSENEx4_Percent", Name:="P274a_NSENEx4_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P274a_NSENEx4_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9eed8c91dcd24c54bf5d61dc90f87caf")>
    <CalculationSpecification(Id:="P275_InYearEx4Subtotal", Name:="P275_InYearEx4Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P275_InYearEx4Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="81db8d8171084a28ba6b34f17e042998")>
    <CalculationSpecification(Id:="P277_Ex5Subtotal", Name:="P277_Ex5Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P277_Ex5Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9efb3bccee5846c38f323251d8ddb6b7")>
    <CalculationSpecification(Id:="P278_NSENEx5", Name:="P278_NSENEx5")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P278_NSENEx5 As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a22c1c9efa794a1294bec1b359b6ad13")>
    <CalculationSpecification(Id:="P278a_NSENEx5_Percent", Name:="P278a_NSENEx5_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P278a_NSENEx5_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="75cc590c83a741cfbb797f5ba7ef07ce")>
    <CalculationSpecification(Id:="P279_InYearEx5Subtotal", Name:="P279_InYearEx5Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P279_InYearEx5Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="575e144723194b6e8531f6258b3b3dc5")>
    <CalculationSpecification(Id:="P281_Ex6Subtotal", Name:="P281_Ex6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P281_Ex6Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b6e27ba8ceb64f628587744e8b982364")>
    <CalculationSpecification(Id:="P282_NSENEx6", Name:="P282_NSENEx6")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P282_NSENEx6 As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d89a2661853f408a9f65e7c13243152a")>
    <CalculationSpecification(Id:="P282a_NSENEx6_Percent", Name:="P282a_NSENEx6_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P282a_NSENEx6_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d57a13bfc72d482a86008216c4ce3182")>
    <CalculationSpecification(Id:="P283_InYearEx6Subtotal", Name:="P283_InYearEx6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P283_InYearEx6Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="0ec1897dfa0f4878bbd943d20050a64a")>
    <CalculationSpecification(Id:="P284_NSENSubtotal", Name:="P284_NSENSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P284_NSENSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ae218a4e7171467b830342226b46324b")>
    <CalculationSpecification(Id:="P285_InYearNSENSubtotal", Name:="P285_InYearNSENSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P285_InYearNSENSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5f55900ef5544a2290252cf64943cea3")>
    <CalculationSpecification(Id:="P286_PriorYearAdjustmentSubtotal", Name:="P286_PriorYearAdjustmentSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P286_PriorYearAdjustmentSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a39ee9302ffc4d26a8c78e3b5582fd8b")>
    <CalculationSpecification(Id:="P287_InYearPriorYearAdjsutmentSubtotal", Name:="P287_InYearPriorYearAdjsutmentSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P287_InYearPriorYearAdjsutmentSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="cbf73f02adf7436bb4bd78cec2ad83b9")>
    <CalculationSpecification(Id:="P298_Growth", Name:="P298_Growth")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P298_Growth As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="7e6aedfbfc8d4eafbe5f7348e3e7b969")>
    <CalculationSpecification(Id:="P299_InYearGrowth", Name:="P299_InYearGrowth")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P299_InYearGrowth As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="029719bd691740d0844af779686c8cf6")>
    <CalculationSpecification(Id:="P300_SBSOutcomeAdjustment", Name:="P300_SBSOutcomeAdjustment")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P300_SBSOutcomeAdjustment As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="85a03b983f4f44be8ff7acb05ab33df9")>
    <CalculationSpecification(Id:="P301_InYearSBSOutcomeAdjustment", Name:="P301_InYearSBSOutcomeAdjustment")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P301_InYearSBSOutcomeAdjustment As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5dc1c10ea7bb4ecfb860e304d5e3adfa")>
    <CalculationSpecification(Id:="P120_PPAindicator", Name:="P120_PPAindicator")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P120_PPAindicator As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a9012351b3314535bef990f617fa2818")>
    <CalculationSpecification(Id:="P121_PPAY5to6Proportion73", Name:="P121_PPAY5to6Proportion73")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P121_PPAY5to6Proportion73 As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="cd40920bac30492994ad57e70a32081f")>
    <CalculationSpecification(Id:="P122_PPAY5to6Proportion78", Name:="P122_PPAY5to6Proportion78")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P122_PPAY5to6Proportion78 As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="c4b4ec49a1c1423cb9a25efb1d57717f")>
    <CalculationSpecification(Id:="P122a_PPAY7378forFAPOnly", Name:="P122a_PPAY7378forFAPOnly")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P122a_PPAY7378forFAPOnly As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="c3728e40a5154847bba1f87de81c177c")>
    <CalculationSpecification(Id:="P123_PPAY1to4ProportionUnder", Name:="P123_PPAY1to4ProportionUnder")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P123_PPAY1to4ProportionUnder As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4d7c1e233ebb4c729e27f3e3bbbcda09")>
    <CalculationSpecification(Id:="P124_PPAY5to6NOR", Name:="P124_PPAY5to6NOR")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P124_PPAY5to6NOR As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="319f067d93254b4b8956671b8e1fa86b")>
    <CalculationSpecification(Id:="P125_PPAY1to4NOR", Name:="P125_PPAY1to4NOR")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P125_PPAY1to4NOR As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="28064856014a42e9b2e0b9eb1fd85d08")>
    <CalculationSpecification(Id:="P126_PPAPriNOR", Name:="P126_PPAPriNOR")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P126_PPAPriNOR As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4ae0615c6b6f495c9964b0d336e817f5")>
    <CalculationSpecification(Id:="P127_PPARate", Name:="P127_PPARate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P127_PPARate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="38ad1f72c9e14fc0adb2b26575237542")>
    <CalculationSpecification(Id:="P128_PPAWeighting", Name:="P128_PPAWeighting")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P128_PPAWeighting As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="c2e109472fb64661b14cdfaf8e3a5a85")>
    <CalculationSpecification(Id:="P129_PPAPupilsY5to6NotAchieving", Name:="P129_PPAPupilsY5to6NotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P129_PPAPupilsY5to6NotAchieving As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9ee0b16ab6e3428aaa39c24904d41de6")>
    <CalculationSpecification(Id:="P130_PPAPupilsY1to4NotAchieving", Name:="P130_PPAPupilsY1to4NotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P130_PPAPupilsY1to4NotAchieving As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1a5db61ce57245b2a2e8a9509903e9c3")>
    <CalculationSpecification(Id:="P131_PPATotalPupilsY1to6NotAchieving", Name:="P131_PPATotalPupilsY1to6NotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P131_PPATotalPupilsY1to6NotAchieving As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6ccb840fbf3944c1a93b46fc44e87b76")>
    <CalculationSpecification(Id:="P132_PPATotalProportionNotAchieving", Name:="P132_PPATotalProportionNotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P132_PPATotalProportionNotAchieving As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="0ba3230401654b56a0209153df8722dc")>
    <CalculationSpecification(Id:="P133_PPATotalFunding", Name:="P133_PPATotalFunding")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P133_PPATotalFunding As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="75b3827acac744de88d36abbdf369be1")>
    <CalculationSpecification(Id:="P134_NSENPPA", Name:="P134_NSENPPA")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P134_NSENPPA As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="984bd1708c214041957ab8e416a69363")>
    <CalculationSpecification(Id:="P134a_NSENPPA_Percent", Name:="P134a_NSENPPA_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P134a_NSENPPA_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="89fb9aab3c584ce0b43b7dfc66f10f38")>
    <CalculationSpecification(Id:="P135_InYearPPASubtotal", Name:="P135_InYearPPASubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P135_InYearPPASubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="27a7364179f54e16a6d8a24346b03035")>
    <CalculationSpecification(Id:="P136_SecPA_Y7Factor", Name:="P136_SecPA_Y7Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P136_SecPA_Y7Factor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="0d5b2b953c3142c2ba9a2869b4d375cf")>
    <CalculationSpecification(Id:="P136a_SecPA_Y7NationalWeight", Name:="P136a_SecPA_Y7NationalWeight")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P136a_SecPA_Y7NationalWeight As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="98ae0fc6c4754a89b488d1d6d15269ec")>
    <CalculationSpecification(Id:="P138_SecPARate", Name:="P138_SecPARate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P138_SecPARate As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6c1015b4cf1d447184563e0ceb5b673a")>
    <CalculationSpecification(Id:="P138a_SecPA_AdjustedSecFactor", Name:="P138a_SecPA_AdjustedSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P138a_SecPA_AdjustedSecFactor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="e310f4f4e8034cf5aa06eaab5613db77")>
    <CalculationSpecification(Id:="P139_SecPASubtotal", Name:="P139_SecPASubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P139_SecPASubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="72f81dbe7b004ffcb9c9bfd3ef5c882d")>
    <CalculationSpecification(Id:="P140_NSENSecPA", Name:="P140_NSENSecPA")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P140_NSENSecPA As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="00d1248ff541429c9b3e4b122024ea50")>
    <CalculationSpecification(Id:="P140a_NSENSecPA_Percent", Name:="P140a_NSENSecPA_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P140a_NSENSecPA_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1a1422c58ae8453fb662cd9664e043d5")>
    <CalculationSpecification(Id:="P141_InYearSecPASubtotal", Name:="P141_InYearSecPASubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P141_InYearSecPASubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a51a1c601e65470d8acc9d8a30dd33a8")>
    <CalculationSpecification(Id:="P185a_Phase", Name:="P185a_Phase")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P185a_Phase As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="eb97b0020bc74c78b6cbd2dd465208e0")>
    <CalculationSpecification(Id:="P186_SparsityTaperFlagPri", Name:="P186_SparsityTaperFlagPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P186_SparsityTaperFlagPri As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="40e109a0d6734ec987c74e7ad8942299")>
    <CalculationSpecification(Id:="P187_SparsityTaperFlagMid", Name:="P187_SparsityTaperFlagMid")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P187_SparsityTaperFlagMid As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="61c70583f88740a5b325bda5f7e0255c")>
    <CalculationSpecification(Id:="P188_SparsityTaperFlagSec", Name:="P188_SparsityTaperFlagSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P188_SparsityTaperFlagSec As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5dff68c1a2914b8cbae263b280c53fda")>
    <CalculationSpecification(Id:="P189_SparsityTaperFlagAllThru", Name:="P189_SparsityTaperFlagAllThru")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P189_SparsityTaperFlagAllThru As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ac5d958b3f0a49e3964f87c1429accd6")>
    <CalculationSpecification(Id:="P190_SparsityUnit", Name:="P190_SparsityUnit")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P190_SparsityUnit As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="369c863a540e4e4eb3734498a9393710")>
    <CalculationSpecification(Id:="P191_SparsityDistance", Name:="P191_SparsityDistance")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P191_SparsityDistance As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ed8cf6602bac4e0280b2009bf4e45b28")>
    <CalculationSpecification(Id:="P192_SparsityDistThreshold", Name:="P192_SparsityDistThreshold")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P192_SparsityDistThreshold As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a1975275a93a45be8333ce3695e6cb95")>
    <CalculationSpecification(Id:="P193_SparsityDistMet_YN", Name:="P193_SparsityDistMet_YN")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P193_SparsityDistMet_YN As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="e0f6606db9784e049d238d27aa101529")>
    <CalculationSpecification(Id:="P194_SparsityAveYGSize", Name:="P194_SparsityAveYGSize")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P194_SparsityAveYGSize As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="15ee6bad258b41fabfdcaca52a0f37c2")>
    <CalculationSpecification(Id:="P195_SparsityYGThreshold", Name:="P195_SparsityYGThreshold")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P195_SparsityYGThreshold As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ceb840b445844a308466f095593ad287")>
    <CalculationSpecification(Id:="P196_SparsityYGThresholdMet_YN", Name:="P196_SparsityYGThresholdMet_YN")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P196_SparsityYGThresholdMet_YN As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1fa6d1e83e1a467bbbd35d52946cffb4")>
    <CalculationSpecification(Id:="P197_SparsityLumpSumSubtotal", Name:="P197_SparsityLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P197_SparsityLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5a1764c5f3ae4e6ba2ae60cc7c8633c7")>
    <CalculationSpecification(Id:="P198_SparsityTaperSubtotal", Name:="P198_SparsityTaperSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P198_SparsityTaperSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="acf3d6f6e8d6465b9f2f48cd57bbca5e")>
    <CalculationSpecification(Id:="P198a_SubtotalLump_Taper_For_FAP_Only", Name:="P198a_SubtotalLump_Taper_For_FAP_Only")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P198a_SubtotalLump_Taper_For_FAP_Only As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="7ad3d4a4dedc41739178ab14920c51f7")>
    <CalculationSpecification(Id:="P199_InYearSparsityLumpSumSubtotal", Name:="P199_InYearSparsityLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P199_InYearSparsityLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="5df85631b1df4f1c80ed765ab91b9b2e")>
    <CalculationSpecification(Id:="P200_InYearSparsityTaperSubtotal", Name:="P200_InYearSparsityTaperSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P200_InYearSparsityTaperSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="31073ecf268a4f12a1089d8b031bd427")>
    <CalculationSpecification(Id:="P200a_InYear_SubtotalLump_Taper_for_FAP_Only", Name:="P200a_InYear_SubtotalLump_Taper_for_FAP_Only")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P200a_InYear_SubtotalLump_Taper_for_FAP_Only As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="8cae4c81b11741559c82bc90c2901c3b")>
    <CalculationSpecification(Id:="P212_PYG", Name:="P212_PYG")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P212_PYG As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ab80aa9662714886a26018ebfadd3341")>
    <CalculationSpecification(Id:="P213_SYG", Name:="P213_SYG")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P213_SYG As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="3c2fc2de276048918273e4c22fa76ebe")>
    <CalculationSpecification(Id:="P236_NSENSparsity", Name:="P236_NSENSparsity")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P236_NSENSparsity As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="30f6d9cc8f2d4ffd9cada5ff787a0cbc")>
    <CalculationSpecification(Id:="P236a_NSENSparsity_Percent", Name:="P236a_NSENSparsity_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P236a_NSENSparsity_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="756634b6fbaf48af880a23ea5f4a4e8f")>
    <CalculationSpecification(Id:="P249_SplitSiteSubtotal", Name:="P249_SplitSiteSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P249_SplitSiteSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f8885a348588475cbad1f2fca57c725e")>
    <CalculationSpecification(Id:="P250_NSENSplitSites", Name:="P250_NSENSplitSites")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P250_NSENSplitSites As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ecbd38b11dab4e80a0c89a09dd4f6676")>
    <CalculationSpecification(Id:="P250a_NSENSplitSites_Percent", Name:="P250a_NSENSplitSites_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P250a_NSENSplitSites_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ecacf297c6c74554be97ac979ead41ff")>
    <CalculationSpecification(Id:="P251_InYearSplitSitesSubtotal", Name:="P251_InYearSplitSitesSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P251_InYearSplitSitesSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="96c4767a671943e9b284f5c15d7d0b12")>
    <CalculationSpecification(Id:="P001_1718DaysOpen", Name:="P001_1718DaysOpen")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P001_1718DaysOpen As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="163cca5519854362b1a3dbbbd715ec5d")>
    <CalculationSpecification(Id:="Lump_Sum_Total", Name:="Lump_Sum_Total")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function Lump_Sum_Total As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="08ec7ceb34e04af090247b6c91fdc043")>
    <CalculationSpecification(Id:="InYearLumpSum", Name:="InYearLumpSum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function InYearLumpSum As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="46009361f8914f309b1d9907212adc97")>
    <CalculationSpecification(Id:="P288_SBSFundingTotal", Name:="P288_SBSFundingTotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P288_SBSFundingTotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="12dd704aa8084606af2e6feb50137b4e")>
    <CalculationSpecification(Id:="P289_InYearSBSFundingTotal", Name:="P289_InYearSBSFundingTotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P289_InYearSBSFundingTotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="571072b923a144db9b03fc52d0bbd5d2")>
    <CalculationSpecification(Id:="P290_ISBTotalSBSFunding", Name:="P290_ISBTotalSBSFunding")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P290_ISBTotalSBSFunding As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b376c9978db94ea98847fae51a06019d")>
    <CalculationSpecification(Id:="P291_TotalPupilLedFactors", Name:="P291_TotalPupilLedFactors")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P291_TotalPupilLedFactors As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="afbb9c054eb44671a6f4aec8b3095f55")>
    <CalculationSpecification(Id:="P292_InYearTotalPupilLedfactors", Name:="P292_InYearTotalPupilLedfactors")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P292_InYearTotalPupilLedfactors As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="caf04369318e47f68244ec20cb620fe2")>
    <CalculationSpecification(Id:="P293_TotalOtherFactors", Name:="P293_TotalOtherFactors")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P293_TotalOtherFactors As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="895f47e704c0442dbebed9474afbcf44")>
    <CalculationSpecification(Id:="P293a_TotalOtherFactors_NoExc", Name:="P293a_TotalOtherFactors_NoExc")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P293a_TotalOtherFactors_NoExc As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2f7058c93883494888f7b74941ca2d99")>
    <CalculationSpecification(Id:="P294_InYearTotalOtherFactors", Name:="P294_InYearTotalOtherFactors")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P294_InYearTotalOtherFactors As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="8fdae9d117d84d6f996d1a569e9a752b")>
    <CalculationSpecification(Id:="P294a_InYearTotalOtherFactors_NoExc", Name:="P294a_InYearTotalOtherFactors_NoExc")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P294a_InYearTotalOtherFactors_NoExc As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1236dd21872d45f2b3bfcd6ba95a934b")>
    <CalculationSpecification(Id:="P295_Dedelegation", Name:="P295_Dedelegation")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P295_Dedelegation As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4b66c5d0393c459fbb576591af226919")>
    <CalculationSpecification(Id:="P296_InYearDedelegation", Name:="P296_InYearDedelegation")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P296_InYearDedelegation As Decimal
        Return Decimal.MinValue
    End Function
End Class
