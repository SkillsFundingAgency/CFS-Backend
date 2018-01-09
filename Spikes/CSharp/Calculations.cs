using System;

public partial class Calculations : BaseCalculation
{
    public CensusPupilCharacteristicsDataset CensusPupilCharacteristics
    {
        get;
        set;
    }

    public LocalAuthorityAveragesDataset LocalAuthorityAverages
    {
        get;
        set;
    }

    public APTProformadatasetDataset APTProformadataset
    {
        get;
        set;
    }

    public APTLocalfactorsdatasetDataset APTLocalfactorsdataset
    {
        get;
        set;
    }

    public APTAdjustedFactorsdatasetDataset APTAdjustedFactorsdataset
    {
        get;
        set;
    }

    public APTFinalBaselinesdatasetDataset APTFinalBaselinesdataset
    {
        get;
        set;
    }

    public APTNewISBdatasetDataset APTNewISBdataset
    {
        get;
        set;
    }

    public APTRecoupmentdatasetDataset APTRecoupmentdataset
    {
        get;
        set;
    }

    public APTInputsandAdjustmentsDataset APTInputsandAdjustments
    {
        get;
        set;
    }

    public _1617_CTCs_DataDataset _1617_CTCs_Data
    {
        get;
        set;
    }

    public _1617_CTCs_ConstantsDataset _1617_CTCs_Constants
    {
        get;
        set;
    }

    public _1617_BasketLAsDataset _1617_BasketLAs
    {
        get;
        set;
    }

    public RecoupmentDataset Recoupment
    {
        get;
        set;
    }

    public RecoupmentNewISBDataset RecoupmentNewISB
    {
        get;
        set;
    }

    [Calculation(Id = "fe8f4c8a418041c88b3c43a428b40195"), CalculationSpecification(Id = "P004_PriRate", Name = "P004_PriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P004_PriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "de523193dcf2414a86b1727ef4a1cf20"), CalculationSpecification(Id = "P005_PriBESubtotal", Name = "P005_PriBESubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P005_PriBESubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "313671c969394dba852164cad297516c"), CalculationSpecification(Id = "P006_NSEN_PriBE", Name = "P006_NSEN_PriBE"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P006_NSEN_PriBE()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e883163c3e9b4aa0b90e0e8e4f60d7d2"), CalculationSpecification(Id = "P006a_NSEN_PriBE_Percent", Name = "P006a_NSEN_PriBE_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P006a_NSEN_PriBE_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "78a7de6510274102ba278b57d53a46dc"), CalculationSpecification(Id = "P007_InYearPriBE_Subtotal", Name = "P007_InYearPriBE_Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P007_InYearPriBE_Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "47194bf2b42241658352913d5d676684"), CalculationSpecification(Id = "P009_KS3Rate", Name = "P009_KS3Rate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P009_KS3Rate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b47f238d9b9543f3b6453fa60c754952"), CalculationSpecification(Id = "P010_KS3_BESubtotal", Name = "P010_KS3_BESubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P010_KS3_BESubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e449560b679c4aa085e629af58495da7"), CalculationSpecification(Id = "P011_NSEN_KS3BE_percent", Name = "P011_NSEN_KS3BE_percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P011_NSEN_KS3BE_percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "568a118680a94a82834875f1c0204ac7"), CalculationSpecification(Id = "P011a_NSEN_KS3BE_Percent", Name = "P011a_NSEN_KS3BE_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P011a_NSEN_KS3BE_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "033a4f64f7ae404eb16fc4c9c5f86a8f"), CalculationSpecification(Id = "P012_InYearKS3_BESubtotal", Name = "P012_InYearKS3_BESubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P012_InYearKS3_BESubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7fe7020df3004be98cccf6fdcb9688a1"), CalculationSpecification(Id = "P014_KS4Rate", Name = "P014_KS4Rate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P014_KS4Rate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "2e10a589b8c447a393184b2c59b30637"), CalculationSpecification(Id = "P015_KS4_BESubtotal", Name = "P015_KS4_BESubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P015_KS4_BESubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "36d227983b43437bbfc0f99dbc12820c"), CalculationSpecification(Id = "P016_NSEN_KS4BE", Name = "P016_NSEN_KS4BE"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P016_NSEN_KS4BE()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d871b3838a4549699a0997586353acd7"), CalculationSpecification(Id = "P016a_NSEN_KS4BE_Percent", Name = "P016a_NSEN_KS4BE_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P016a_NSEN_KS4BE_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "593d6c6337a248fda742916e51eca429"), CalculationSpecification(Id = "P018_InYearKS4_BESubtotal", Name = "P018_InYearKS4_BESubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "bb411f3c107a40a8a71ff57604296e0e", Name = "Basic Entitlement")]
    public decimal P018_InYearKS4_BESubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "662f357e611c40c097b262741e0ee999"), CalculationSpecification(Id = "P297_DedelegationRetained", Name = "P297_DedelegationRetained"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "cc9eac91ede6480eb46f748efb3a9d11", Name = "Dedelegation Retained by LA")]
    public decimal P297_DedelegationRetained()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "46195d68f6ec4d7dae375ae9a546e104"), CalculationSpecification(Id = "P142_EAL1PriFactor", Name = "P142_EAL1PriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P142_EAL1PriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "59d0b78ef30a48898be3636d47a548ca"), CalculationSpecification(Id = "P144_EAL1PriRate", Name = "P144_EAL1PriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P144_EAL1PriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "390aa119f1f846e99d7c23c8c9dba92a"), CalculationSpecification(Id = "P145_EAL1PriSubtotal", Name = "P145_EAL1PriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P145_EAL1PriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b194a092816c40e0955397872ea6b644"), CalculationSpecification(Id = "P146_InYearEAL1PriSubtotal", Name = "P146_InYearEAL1PriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P146_InYearEAL1PriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "fd1478abbd4140ceb7b20a0d3fc4538c"), CalculationSpecification(Id = "P147_EAL2PriFactor", Name = "P147_EAL2PriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P147_EAL2PriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "64cb23ded6d34302bcfc084fe360bec9"), CalculationSpecification(Id = "P149_EAL2PriRate", Name = "P149_EAL2PriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P149_EAL2PriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "00cbab65cc964c8195e21d376555b73b"), CalculationSpecification(Id = "P150_EAL2PriSubtotal", Name = "P150_EAL2PriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P150_EAL2PriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d7dc12ccae3741d69057b9955497dd44"), CalculationSpecification(Id = "P151_InYearEAL2PriSubtotal", Name = "P151_InYearEAL2PriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P151_InYearEAL2PriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "38ee2d42263e4fe3bc5666bd88deee37"), CalculationSpecification(Id = "P152_EAL3PriFactor", Name = "P152_EAL3PriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P152_EAL3PriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c2bd943f8e95499b959d43b118d48b99"), CalculationSpecification(Id = "P154_EAL3PriRate", Name = "P154_EAL3PriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P154_EAL3PriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "0329f31139594a809604282b0263de20"), CalculationSpecification(Id = "P155_EAL3PriSubtotal", Name = "P155_EAL3PriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P155_EAL3PriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7878fde442b44f04a87f411468b85733"), CalculationSpecification(Id = "P156_NSENPriEAL", Name = "P156_NSENPriEAL"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P156_NSENPriEAL()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "1c1fd6edff31479c8a092df86a968900"), CalculationSpecification(Id = "P156a_NSENPriEAL_Percent", Name = "P156a_NSENPriEAL_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P156a_NSENPriEAL_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "0edc495108224b84890e827a997a29a6"), CalculationSpecification(Id = "P157_InYearEAL3PriSubtotal", Name = "P157_InYearEAL3PriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P157_InYearEAL3PriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "9f839b2e10d341bb9043a7d7dd0ba3c5"), CalculationSpecification(Id = "P158_EAL1SecFactor", Name = "P158_EAL1SecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P158_EAL1SecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "2c069c2eae0e4ce88ac1580e91025c19"), CalculationSpecification(Id = "P160_EAL1SecRate", Name = "P160_EAL1SecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P160_EAL1SecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "a3a676cb0cbe4b4a8168394953f10a58"), CalculationSpecification(Id = "P161_EAL1SecSubtotal", Name = "P161_EAL1SecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P161_EAL1SecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c6670c684c75472cb7f48166a6a583f6"), CalculationSpecification(Id = "P162_InYearEAL1SecSubtotal", Name = "P162_InYearEAL1SecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P162_InYearEAL1SecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "988349a10f084ad9a98696e0cc171c1d"), CalculationSpecification(Id = "P163_EAL2SecFactor", Name = "P163_EAL2SecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P163_EAL2SecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "540d8bd29de9415eb535a2cc2b48c07e"), CalculationSpecification(Id = "P165_EAL2SecRate", Name = "P165_EAL2SecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P165_EAL2SecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7fd6b0ec3f024097a6332ac919b048d7"), CalculationSpecification(Id = "P166_EAL2SecSubtotal", Name = "P166_EAL2SecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P166_EAL2SecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7d69928d844245058b21e34f065e2384"), CalculationSpecification(Id = "P167_InYearEAL2SecSubtotal", Name = "P167_InYearEAL2SecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P167_InYearEAL2SecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b5f4c603263b4377b16527f8b4d157b7"), CalculationSpecification(Id = "P168_EAL3SecFactor", Name = "P168_EAL3SecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P168_EAL3SecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "5117a44bfb654b28ac58cb81c1d12307"), CalculationSpecification(Id = "P170_EAL3SecRate", Name = "P170_EAL3SecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P170_EAL3SecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "294856ca77f3472fb45f26d1980e803f"), CalculationSpecification(Id = "P171_EAL3SecSubtotal", Name = "P171_EAL3SecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P171_EAL3SecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f35fdc45959e463e8d06125e1990ee30"), CalculationSpecification(Id = "P172_NSENSecEAL", Name = "P172_NSENSecEAL"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P172_NSENSecEAL()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "8a4b8d3c0560475a91e6b1222b9783ea"), CalculationSpecification(Id = "P172a_NSENSecEAL_Percent", Name = "P172a_NSENSecEAL_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P172a_NSENSecEAL_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "5890fcc091724cb1a3601fe537958fbf"), CalculationSpecification(Id = "P173_InYearEAL3SecSubtotal", Name = "P173_InYearEAL3SecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "dfd825e4a5784fa188379b080fa0c4ae", Name = "EAL")]
    public decimal P173_InYearEAL3SecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "83a7940fb55a47d4b7af5c3bb495d1fa"), CalculationSpecification(Id = "P019_PriFSMFactor", Name = "P019_PriFSMFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P019_PriFSMFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b83c8f1227384d5dbeac546ed623ce5f"), CalculationSpecification(Id = "P021_PriFSMRate", Name = "P021_PriFSMRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P021_PriFSMRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7d7247b9cfc841129b68a0d1d6225dca"), CalculationSpecification(Id = "P022_PriFSMSubtotal", Name = "P022_PriFSMSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P022_PriFSMSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "fa10cd9828624f3aa240bb83e34f3048"), CalculationSpecification(Id = "P023_InYearPriFSMSubtotal", Name = "P023_InYearPriFSMSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P023_InYearPriFSMSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "6be9e4196c0a47a195a02be23d2db98d"), CalculationSpecification(Id = "P024_PriFSM6Factor", Name = "P024_PriFSM6Factor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P024_PriFSM6Factor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "24725de0aabc48458bb11872aeca869b"), CalculationSpecification(Id = "P026_PriFSM6Rate", Name = "P026_PriFSM6Rate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P026_PriFSM6Rate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "2bbb7f7f7db042788c5077f211102d80"), CalculationSpecification(Id = "P027_PriFSM6Subtotal", Name = "P027_PriFSM6Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P027_PriFSM6Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4a728870ec7c45909d49f8b277c4e331"), CalculationSpecification(Id = "P028_NSENFSMPri", Name = "P028_NSENFSMPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P028_NSENFSMPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3663c935be6d4ac7acf92bf43a8241ba"), CalculationSpecification(Id = "P028a_NSENFSMPri_Percent", Name = "P028a_NSENFSMPri_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P028a_NSENFSMPri_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "91e5671e04c14d998d667a89f140ee17"), CalculationSpecification(Id = "P029_InYearPriFSM6Subtotal", Name = "P029_InYearPriFSM6Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P029_InYearPriFSM6Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "1946b54b5a17410c816e0487b057d317"), CalculationSpecification(Id = "P030_SecFSMFactor", Name = "P030_SecFSMFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P030_SecFSMFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "eaf7da9ba52d491a95c26e55a0f9a4bc"), CalculationSpecification(Id = "P032_SecFSMRate", Name = "P032_SecFSMRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P032_SecFSMRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "5e53c2bce0d942b1804ff0c467aa07b8"), CalculationSpecification(Id = "P033_SecFSMSubtotal", Name = "P033_SecFSMSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P033_SecFSMSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b3195203aedc40edbc2b9a8d7823dcc4"), CalculationSpecification(Id = "P034_InYearSecFSMSubtotal", Name = "P034_InYearSecFSMSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P034_InYearSecFSMSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "79c8104590bf416c8cdf8bb115de96f9"), CalculationSpecification(Id = "P035_SecFSM6Factor", Name = "P035_SecFSM6Factor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P035_SecFSM6Factor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "02227c0ddc344127b8898749b48f5036"), CalculationSpecification(Id = "P037_SecFSM6Rate", Name = "P037_SecFSM6Rate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P037_SecFSM6Rate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b274239ca98c4b3b847cf16192eb6d34"), CalculationSpecification(Id = "P038_SecFSM6Subtotal", Name = "P038_SecFSM6Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P038_SecFSM6Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "49e763aae5634feb87ca91f2cf202d01"), CalculationSpecification(Id = "P039_NSENFSMSec", Name = "P039_NSENFSMSec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P039_NSENFSMSec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "07fdaffd6f1b45faa135c8c6969db61f"), CalculationSpecification(Id = "P039a_NSENFSMSec_Percent", Name = "P039a_NSENFSMSec_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P039a_NSENFSMSec_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3f0b4312ab484736a255c85b61a69d98"), CalculationSpecification(Id = "P040_InYearSecFSM6Subtotal", Name = "P040_InYearSecFSM6Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "524c10bb461b46f3b351b86403168379", Name = "FSM")]
    public decimal P040_InYearSecFSM6Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "66e691e720f24faabb40c20e65ea43a6"), CalculationSpecification(Id = "P041_IDACIFPriFactor", Name = "P041_IDACIFPriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P041_IDACIFPriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "fa56931248a64054a4c19e4dd8a2f689"), CalculationSpecification(Id = "P043_IDACIFPriRate", Name = "P043_IDACIFPriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P043_IDACIFPriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "91a7a29a0f0f4a9f9488946162ac7614"), CalculationSpecification(Id = "P044_IDACIFPriSubtotal", Name = "P044_IDACIFPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P044_IDACIFPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "6456eea8c04443c2bd130e4da4c22d2f"), CalculationSpecification(Id = "P045_NSENIDACIFPri", Name = "P045_NSENIDACIFPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P045_NSENIDACIFPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "5f3b44fec18a41269f0c5bdc0d9b1565"), CalculationSpecification(Id = "P045a_NSENIDACIFPri_Percent", Name = "P045a_NSENIDACIFPri_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P045a_NSENIDACIFPri_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e346d1d9cefe46f5b5fda2ed672ba242"), CalculationSpecification(Id = "P046_InYearIDACIFPriSubtotal", Name = "P046_InYearIDACIFPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P046_InYearIDACIFPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c804e06998014272812220567964b0f3"), CalculationSpecification(Id = "P047_IDACIEPriFactor", Name = "P047_IDACIEPriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P047_IDACIEPriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "1069ee97db8c40939be48e450976dab0"), CalculationSpecification(Id = "P049_IDACIEPriRate", Name = "P049_IDACIEPriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P049_IDACIEPriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "28a940899b4b4491a54ca8b6f255a82e"), CalculationSpecification(Id = "P050_IDACIEPriSubtotal", Name = "P050_IDACIEPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P050_IDACIEPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "684d321a79494628b872308104a8c734"), CalculationSpecification(Id = "P051_NSENIDACIEPri", Name = "P051_NSENIDACIEPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P051_NSENIDACIEPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "917ff83d78fd4e58981b679b331595b6"), CalculationSpecification(Id = "P051a_NSENIDACIEPri_Percent", Name = "P051a_NSENIDACIEPri_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P051a_NSENIDACIEPri_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "eea31fc3b92842658e084f017e2f4b6a"), CalculationSpecification(Id = "P052_InYearIDACIEPriSubtotal", Name = "P052_InYearIDACIEPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P052_InYearIDACIEPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4b9adf9fe9d54a5b9e71b1d812400d86"), CalculationSpecification(Id = "P053_IDACIDPriFactor", Name = "P053_IDACIDPriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P053_IDACIDPriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "dff661ac9eb848248ddc63aa3bf9b792"), CalculationSpecification(Id = "P055_IDACIDPriRate", Name = "P055_IDACIDPriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P055_IDACIDPriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "ad668a5ae2974cc18d6991bce3be0c14"), CalculationSpecification(Id = "P056_IDACIDPriSubtotal", Name = "P056_IDACIDPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P056_IDACIDPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "a494b8dce52c4492a30051d93b48e768"), CalculationSpecification(Id = "P057_NSENIDACIDPri", Name = "P057_NSENIDACIDPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P057_NSENIDACIDPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "eb9b002bdbec484c8794375d6568a640"), CalculationSpecification(Id = "P057a_NSENIDACIDPri_Percent", Name = "P057a_NSENIDACIDPri_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P057a_NSENIDACIDPri_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3c027295a46d459980d003267a3bf095"), CalculationSpecification(Id = "P058_InYearIDACIDPriSubtotal", Name = "P058_InYearIDACIDPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P058_InYearIDACIDPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "2a24e4cbed2c4309ba5dc8062057934f"), CalculationSpecification(Id = "P059_IDACICPriFactor", Name = "P059_IDACICPriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P059_IDACICPriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "232d0c3070004c6fa9b173a52ec77668"), CalculationSpecification(Id = "P061_IDACICPriRate", Name = "P061_IDACICPriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P061_IDACICPriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e0afd9a625b64701917ba51f63ec28d9"), CalculationSpecification(Id = "P062_IDACICPriSubtotal", Name = "P062_IDACICPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P062_IDACICPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "87664db92c4d49558b54507c730f0eb3"), CalculationSpecification(Id = "P063_NSENIDACICPri", Name = "P063_NSENIDACICPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P063_NSENIDACICPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "9659e8965cfe4f98b3bff2bd7e5f70f6"), CalculationSpecification(Id = "P063a_NSENIDACICPri_Percent", Name = "P063a_NSENIDACICPri_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P063a_NSENIDACICPri_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4b2009b070ed45b49c1bfa29c5123c89"), CalculationSpecification(Id = "P064_InYearIDACICPriSubtotal", Name = "P064_InYearIDACICPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P064_InYearIDACICPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f34468f0da944ce0aa2ced6c7b8842f3"), CalculationSpecification(Id = "P065_IDACIBPriFactor", Name = "P065_IDACIBPriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P065_IDACIBPriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7d4ff12b38b6438781e70c7f104bdffe"), CalculationSpecification(Id = "P067_IDACIBPriRate", Name = "P067_IDACIBPriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P067_IDACIBPriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "541b014024cc4690881cc3a624a2d62e"), CalculationSpecification(Id = "P068_IDACIBPriSubtotal", Name = "P068_IDACIBPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P068_IDACIBPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e38159982d904796b8467ce97930e966"), CalculationSpecification(Id = "P069_NSENIDACIBPri", Name = "P069_NSENIDACIBPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P069_NSENIDACIBPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4b722db75b524141b298a9e19a31779a"), CalculationSpecification(Id = "P069a_NSENIDACIBPri_Percent", Name = "P069a_NSENIDACIBPri_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P069a_NSENIDACIBPri_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "9e91c82f31394fd987acdc1a081ce0ec"), CalculationSpecification(Id = "P070_InYearIDACIBPriSubtotal", Name = "P070_InYearIDACIBPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P070_InYearIDACIBPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "5f94d21407864a26854b8ecc80ee3aae"), CalculationSpecification(Id = "P071_IDACIAPriFactor", Name = "P071_IDACIAPriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P071_IDACIAPriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f550b4863c1b41f29c7557a7d22d7036"), CalculationSpecification(Id = "P073_IDACIAPriRate", Name = "P073_IDACIAPriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P073_IDACIAPriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "0a7ae339d1b54f22a688c461403bed39"), CalculationSpecification(Id = "P074_IDACIAPriSubtotal", Name = "P074_IDACIAPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P074_IDACIAPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "9c5ef0098b03437087c3191c1adea4f0"), CalculationSpecification(Id = "P075_NSENIDACIAPri", Name = "P075_NSENIDACIAPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P075_NSENIDACIAPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e6099c4b9f4a41c0804e919890de31d6"), CalculationSpecification(Id = "P075a_NSENIDACIAPri_Percent", Name = "P075a_NSENIDACIAPri_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P075a_NSENIDACIAPri_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "372fc975ac6b49fb95a7b8595d31aa3d"), CalculationSpecification(Id = "P076_InYearIDACIAPriSubtotal", Name = "P076_InYearIDACIAPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P076_InYearIDACIAPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "9811c33ef503431c8989a846485d22d8"), CalculationSpecification(Id = "P077_IDACIFSecFactor", Name = "P077_IDACIFSecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P077_IDACIFSecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "31efde1b1e8841dbbc7bb6e0f2fe252f"), CalculationSpecification(Id = "P079_IDACIFSecRate", Name = "P079_IDACIFSecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P079_IDACIFSecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "97d005121c144e93adcec2bbdff2185a"), CalculationSpecification(Id = "P080_IDACIFSecSubtotal", Name = "P080_IDACIFSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P080_IDACIFSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "6c61efab5f904908b9f6f5fdafc7be65"), CalculationSpecification(Id = "P081_NSENIDACIFSec", Name = "P081_NSENIDACIFSec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P081_NSENIDACIFSec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3b3f55b1890e4d1a90e8f2537efe497e"), CalculationSpecification(Id = "P081a_NSENIDACIFSec_Percent", Name = "P081a_NSENIDACIFSec_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P081a_NSENIDACIFSec_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "fda71bf45d274e21bc934bc487875efb"), CalculationSpecification(Id = "P082_InYearIDACIFSecSubtotal", Name = "P082_InYearIDACIFSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P082_InYearIDACIFSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "68d44675264c4d15a8d9c36bb39ea188"), CalculationSpecification(Id = "P083_IDACIESecFactor", Name = "P083_IDACIESecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P083_IDACIESecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "418dc23c6b0040e88c61582b63c0aa15"), CalculationSpecification(Id = "P085_IDACIESecRate", Name = "P085_IDACIESecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P085_IDACIESecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "ff4c5c2a51d94faf98ffba936dfbdf79"), CalculationSpecification(Id = "P086_IDACIESecSubtotal", Name = "P086_IDACIESecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P086_IDACIESecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "24ee2acba5de4b2eb2db636ae3ea731e"), CalculationSpecification(Id = "P087_NSENIDACIESec", Name = "P087_NSENIDACIESec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P087_NSENIDACIESec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "08df764418414619b90d8e69ab051d01"), CalculationSpecification(Id = "P87a_NSENIDACIESec_Percent", Name = "P87a_NSENIDACIESec_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P87a_NSENIDACIESec_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d14d86a6665d4b17a408a3cae0166a27"), CalculationSpecification(Id = "P088_InYearIDACIESecSubtotal", Name = "P088_InYearIDACIESecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P088_InYearIDACIESecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "40808aed5c0a4053aebb9cf2403c0adb"), CalculationSpecification(Id = "P089_IDACIDSecFactor", Name = "P089_IDACIDSecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P089_IDACIDSecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f4d4cdb468824dde8db10aeffd456471"), CalculationSpecification(Id = "P091_IDACIDSecRate", Name = "P091_IDACIDSecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P091_IDACIDSecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c81ac29d3b4d4ed79430f05b396b4e36"), CalculationSpecification(Id = "P092_IDACIDSecSubtotal", Name = "P092_IDACIDSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P092_IDACIDSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4ae30c1e6b95456ea0e4fa18cfcd2c08"), CalculationSpecification(Id = "P093_NSENIDACIDSec", Name = "P093_NSENIDACIDSec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P093_NSENIDACIDSec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4bad64ac4e554dde8a53154fe0de34d2"), CalculationSpecification(Id = "P093a_NSENIDACIDSec_Percent", Name = "P093a_NSENIDACIDSec_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P093a_NSENIDACIDSec_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "6c5f044900d54efa8a89169060c7cdc2"), CalculationSpecification(Id = "P094_InYearIDACIDSecSubtotal", Name = "P094_InYearIDACIDSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P094_InYearIDACIDSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f77f919355304f3b9b035fa8be7c9a46"), CalculationSpecification(Id = "P095_IDACICSecFactor", Name = "P095_IDACICSecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P095_IDACICSecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "34a455ea2eb34973a6f2a7628a543db8"), CalculationSpecification(Id = "P097_IDACICSecRate", Name = "P097_IDACICSecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P097_IDACICSecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f032f2e61a46437eb89c829b94f1129a"), CalculationSpecification(Id = "P098_IDACICSecSubtotal", Name = "P098_IDACICSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P098_IDACICSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "0ad5fa30bbf54ccfae7caf274de447ff"), CalculationSpecification(Id = "P099_NSENIDACICSec", Name = "P099_NSENIDACICSec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P099_NSENIDACICSec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d34923ac0676406cb4ce14bc664e0e3d"), CalculationSpecification(Id = "P099a_NSENIDACICSec_Percent", Name = "P099a_NSENIDACICSec_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P099a_NSENIDACICSec_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "1a82ee8378cc4c2c9ce618a2fc11d19b"), CalculationSpecification(Id = "P100_InYearIDACICSecSubtotal", Name = "P100_InYearIDACICSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P100_InYearIDACICSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d8cc81a02d0e4baeadf1b301bc586c85"), CalculationSpecification(Id = "P101_IDACIBSecFactor", Name = "P101_IDACIBSecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P101_IDACIBSecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c9fc896117fc45f88a8dc8ae62883728"), CalculationSpecification(Id = "P103_IDACIBSecRate", Name = "P103_IDACIBSecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P103_IDACIBSecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4f41443b79b64eaca21724d632469b40"), CalculationSpecification(Id = "P104_IDACIBSecSubtotal", Name = "P104_IDACIBSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P104_IDACIBSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b3077ec56442419ebf5ec265d745e855"), CalculationSpecification(Id = "P105_NSENIDACIBSec", Name = "P105_NSENIDACIBSec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P105_NSENIDACIBSec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "288130fb04114cf0ba8a51eecc211336"), CalculationSpecification(Id = "P105a_NSENIDACIBSec_Percent", Name = "P105a_NSENIDACIBSec_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P105a_NSENIDACIBSec_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "2939a94785e7463688343386b2c74e01"), CalculationSpecification(Id = "P106_InYearIDACIBSecSubtotal", Name = "P106_InYearIDACIBSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P106_InYearIDACIBSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "1853a469b9334cd0909d1bc9c4de3091"), CalculationSpecification(Id = "P107_IDACIASecFactor", Name = "P107_IDACIASecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P107_IDACIASecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "64cad6b2d0d64a8bb7cc147d9c71219d"), CalculationSpecification(Id = "P109_IDACIASecRate", Name = "P109_IDACIASecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P109_IDACIASecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "872b6f25f2fb4625a7f7e966717f2214"), CalculationSpecification(Id = "P110_IDACIASecSubtotal", Name = "P110_IDACIASecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P110_IDACIASecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "680b65d82bb14ca6837312ddf629faf5"), CalculationSpecification(Id = "P111_NSENIDACIASec", Name = "P111_NSENIDACIASec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P111_NSENIDACIASec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "54e3bccba5644c00adb0d9a62aca6083"), CalculationSpecification(Id = "P111a_NSENIDACIASec_Percent", Name = "P111a_NSENIDACIASec_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P111a_NSENIDACIASec_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "482cd931e5164cbeba816f3cc4aa2755"), CalculationSpecification(Id = "P112_InYearIDACIASecSubtotal", Name = "P112_InYearIDACIASecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "86e2181721da4a669a09cccbafc502f1", Name = "IDACI")]
    public decimal P112_InYearIDACIASecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "03c5830d15fd4942af518440d212efa8"), CalculationSpecification(Id = "P114_LACFactor", Name = "P114_LACFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "3e4d786c0a984c6c9eb92a959ef1e5f5", Name = "LAC")]
    public decimal P114_LACFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "5e66b9d8cfbe4f91a45c82979819c2fc"), CalculationSpecification(Id = "P116_LACRate", Name = "P116_LACRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "3e4d786c0a984c6c9eb92a959ef1e5f5", Name = "LAC")]
    public decimal P116_LACRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4c70d9b30fb54d7294e348a178f8b8f6"), CalculationSpecification(Id = "P117_LACSubtotal", Name = "P117_LACSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "3e4d786c0a984c6c9eb92a959ef1e5f5", Name = "LAC")]
    public decimal P117_LACSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "aa69c2de4f7c49a498c699b3ddc6f035"), CalculationSpecification(Id = "P118_NSENLAC", Name = "P118_NSENLAC"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "3e4d786c0a984c6c9eb92a959ef1e5f5", Name = "LAC")]
    public decimal P118_NSENLAC()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "fd8e0a9c7abd44dd8bf3c32253f9a3ef"), CalculationSpecification(Id = "P118a_NSENLAC_Percent", Name = "P118a_NSENLAC_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "3e4d786c0a984c6c9eb92a959ef1e5f5", Name = "LAC")]
    public decimal P118a_NSENLAC_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "18a335103ed84bd7bc07b9ff132f4637"), CalculationSpecification(Id = "P119_InYearLACSubtotal", Name = "P119_InYearLACSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "3e4d786c0a984c6c9eb92a959ef1e5f5", Name = "LAC")]
    public decimal P119_InYearLACSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "8f868d41cb8a4af5ab891bc3a33f2675"), CalculationSpecification(Id = "P174_MobPriFactor", Name = "P174_MobPriFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P174_MobPriFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "530c7386b6d3422ba1d20730238b7a1d"), CalculationSpecification(Id = "P176_MobPriRate", Name = "P176_MobPriRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P176_MobPriRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "1d8d810c52c04eac9454207c45aa217d"), CalculationSpecification(Id = "P177_MobPriSubtotal", Name = "P177_MobPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P177_MobPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "faa4b55d66004dcb84de6762dca331cb"), CalculationSpecification(Id = "P178_NSENMobPri", Name = "P178_NSENMobPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P178_NSENMobPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3ead28480b1243d69dc2c303b8615eb6"), CalculationSpecification(Id = "P178a_NSENMobPri_Percent", Name = "P178a_NSENMobPri_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P178a_NSENMobPri_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "6d4b99ae95a94911946f13f0a9840e4a"), CalculationSpecification(Id = "P179_InYearMobPriSubtotal", Name = "P179_InYearMobPriSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P179_InYearMobPriSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "260c020c9ee442fba35f2b7077210fc5"), CalculationSpecification(Id = "P180_MobSecFactor", Name = "P180_MobSecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P180_MobSecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "8f924a46bcbb4cbf92e34bbb44373eaa"), CalculationSpecification(Id = "P182_MobSecRate", Name = "P182_MobSecRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P182_MobSecRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "30eb0cd986c9447699a2670468a9e9a3"), CalculationSpecification(Id = "P183_MobSecSubtotal", Name = "P183_MobSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P183_MobSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b737e799d66141d8953b3dccad074a72"), CalculationSpecification(Id = "P184_NSENMobSec", Name = "P184_NSENMobSec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P184_NSENMobSec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3ec1f106dc0644cb89fec49256c53cca"), CalculationSpecification(Id = "P184a_NSENMobSec_Percent", Name = "P184a_NSENMobSec_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P184a_NSENMobSec_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "0ede897362a149fa8b14fe1b6475b770"), CalculationSpecification(Id = "P185_InYearMobSecSubtotal", Name = "P185_InYearMobSecSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41e8854d7b7e4ecdb46bbc643a62d19e", Name = "Mobility")]
    public decimal P185_InYearMobSecSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "74ac2ea4cf10414ba462a0d2d290a70d"), CalculationSpecification(Id = "P239_PriLumpSumFactor", Name = "P239_PriLumpSumFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P239_PriLumpSumFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "fab209a2917941c4a2121a9ea1a3fd6f"), CalculationSpecification(Id = "P240_PriLumpSumRate", Name = "P240_PriLumpSumRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P240_PriLumpSumRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c98398cb58514e5da84918fde34a9832"), CalculationSpecification(Id = "P241_Primary_Lump_Sum", Name = "P241_Primary_Lump_Sum"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P241_Primary_Lump_Sum()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e15c6dbdc3ca45d78444f384abb82cbb"), CalculationSpecification(Id = "P242_InYearPriLumpSumSubtotal", Name = "P242_InYearPriLumpSumSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P242_InYearPriLumpSumSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "535f957aee774423aa9acfb232af640b"), CalculationSpecification(Id = "P243_SecLumpSumFactor", Name = "P243_SecLumpSumFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P243_SecLumpSumFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7f44064655324dcbb4d8835d96e98c9c"), CalculationSpecification(Id = "P244_SecLumpSumRate", Name = "P244_SecLumpSumRate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P244_SecLumpSumRate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "17473599d60d4052b841ea3b6f676f68"), CalculationSpecification(Id = "P245_Secondary_Lump_Sum", Name = "P245_Secondary_Lump_Sum"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P245_Secondary_Lump_Sum()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "48acc67ef83746eb84bf2d97a80d3a30"), CalculationSpecification(Id = "P246_In YearSecLumpSumSubtotal", Name = "P246_In YearSecLumpSumSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P246_InYearSecLumpSumSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "5d9ad3ce94484bd78766d9abbda3c390"), CalculationSpecification(Id = "P247_NSENLumpSumPri", Name = "P247_NSENLumpSumPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P247_NSENLumpSumPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e7bc31fca1974542bf57857ddc5f489a"), CalculationSpecification(Id = "P247a_NSENLumpSumPri_Percent", Name = "P247a_NSENLumpSumPri_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P247a_NSENLumpSumPri_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "27d7737063994977ac440c552c042468"), CalculationSpecification(Id = "P248_NSENLumpSumSec", Name = "P248_NSENLumpSumSec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P248_NSENLumpSumSec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7c29f656a19a4ae5b60abef378a4521b"), CalculationSpecification(Id = "P248a_NSENLumpSumSec_Percent", Name = "P248a_NSENLumpSumSec_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P248a_NSENLumpSumSec_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "41903947e8ed46cb801c54ea750a4ea7"), CalculationSpecification(Id = "P252_PFISubtotal", Name = "P252_PFISubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P252_PFISubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "9d662a865c174bf0984443e38ca4e331"), CalculationSpecification(Id = "P253_NSENPFI", Name = "P253_NSENPFI"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P253_NSENPFI()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "796010621358482a96f871efc94e3625"), CalculationSpecification(Id = "P253a_NSENPFI_Percent", Name = "P253a_NSENPFI_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P253a_NSENPFI_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4f3e06c93b3746f1aa3f719a9a3c44a6"), CalculationSpecification(Id = "P254_InYearPFISubtotal", Name = "P254_InYearPFISubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P254_InYearPFISubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b65cee7ca7084049b210484087a54a1c"), CalculationSpecification(Id = "P255_FringeSubtotal", Name = "P255_FringeSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P255_FringeSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "b6f078d2a2f344c5b32fd9c03382c48b"), CalculationSpecification(Id = "P257_InYearFringeSubtotal", Name = "P257_InYearFringeSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P257_InYearFringeSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "8ca6e436e68f4565bff90d319ab94177"), CalculationSpecification(Id = "P261_Ex1Subtotal", Name = "P261_Ex1Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P261_Ex1Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "261e2eb7c67642ffa1e106e26a205dc8"), CalculationSpecification(Id = "P262_NSENEx1", Name = "P262_NSENEx1"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P262_NSENEx1()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "eeb5541339ca4d1e84bb32a15d31557c"), CalculationSpecification(Id = "P262a_NSENEx1_Percent", Name = "P262a_NSENEx1_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P262a_NSENEx1_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "da11e9e29dbb47ae968ccc36f938d2ef"), CalculationSpecification(Id = "P264_InYearEx1Subtotal", Name = "P264_InYearEx1Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P264_InYearEx1Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "1807fbcd5a56427982ce21eab892445a"), CalculationSpecification(Id = "P265_Ex2Subtotal", Name = "P265_Ex2Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P265_Ex2Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "36dbee8593f848b887d67f8b778f91b5"), CalculationSpecification(Id = "P266_NSENEx2", Name = "P266_NSENEx2"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P266_NSENEx2()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "76fe66d0dba141848137e54bdd1d557f"), CalculationSpecification(Id = "P266a_NSENEx2_Percent", Name = "P266a_NSENEx2_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P266a_NSENEx2_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "dffd60893ad14cdfaff40151dd6c2a08"), CalculationSpecification(Id = "P267_InYearEx2Subtotal", Name = "P267_InYearEx2Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P267_InYearEx2Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "877b1a8b9aa7423abfc379f8c0cd559c"), CalculationSpecification(Id = "P269_Ex3Subtotal", Name = "P269_Ex3Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P269_Ex3Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "dbf0bbe8876f4563b4df095ebc098e14"), CalculationSpecification(Id = "P270_NSENEx3", Name = "P270_NSENEx3"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P270_NSENEx3()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d40ac5278d974722a2480057c284f9b0"), CalculationSpecification(Id = "P270a_NSENEx3_Percent", Name = "P270a_NSENEx3_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P270a_NSENEx3_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "621ca59787ab4f25b68de3fa4191e9ef"), CalculationSpecification(Id = "P271_InYearEx3Subtotal", Name = "P271_InYearEx3Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P271_InYearEx3Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "12da37ac567148629c64d2668f908032"), CalculationSpecification(Id = "P273_Ex4Subtotal", Name = "P273_Ex4Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P273_Ex4Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "ab0c522b1b2e4ae0929339de3d7b8b1e"), CalculationSpecification(Id = "P274_NSENEx4", Name = "P274_NSENEx4"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P274_NSENEx4()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "de986af1ff3a4c6b80b96430e9b36ce7"), CalculationSpecification(Id = "P274a_NSENEx4_Percent", Name = "P274a_NSENEx4_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P274a_NSENEx4_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "9bbe4aa0ea0f4c1ab4380a9a4ecba3fa"), CalculationSpecification(Id = "P275_InYearEx4Subtotal", Name = "P275_InYearEx4Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P275_InYearEx4Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "774c0d31e84f4fcaa6c35fc62d5082ae"), CalculationSpecification(Id = "P277_Ex5Subtotal", Name = "P277_Ex5Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P277_Ex5Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d29a40fb349e434397204eff93853728"), CalculationSpecification(Id = "P278_NSENEx5", Name = "P278_NSENEx5"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P278_NSENEx5()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "1c03a5296b4d44e38616ead43abd1b1d"), CalculationSpecification(Id = "P278a_NSENEx5_Percent", Name = "P278a_NSENEx5_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P278a_NSENEx5_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d89ecc2a30b948bb8dd13ae7998ce80d"), CalculationSpecification(Id = "P279_InYearEx5Subtotal", Name = "P279_InYearEx5Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P279_InYearEx5Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "95ec407cd70042c7899bf6c95e8c2a3d"), CalculationSpecification(Id = "P281_Ex6Subtotal", Name = "P281_Ex6Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P281_Ex6Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d6d20719e3d34420ba8db7e55903e576"), CalculationSpecification(Id = "P282_NSENEx6", Name = "P282_NSENEx6"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P282_NSENEx6()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "a20a74d060764cc7af55be7477b318a6"), CalculationSpecification(Id = "P282a_NSENEx6_Percent", Name = "P282a_NSENEx6_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P282a_NSENEx6_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f500f146564a4feb85ff20e8d85e076a"), CalculationSpecification(Id = "P283_InYearEx6Subtotal", Name = "P283_InYearEx6Subtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P283_InYearEx6Subtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c947cb76f16f4197b5962a21c769c50e"), CalculationSpecification(Id = "P284_NSENSubtotal", Name = "P284_NSENSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P284_NSENSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "87442421b637406d990fb28cfe983695"), CalculationSpecification(Id = "P285_InYearNSENSubtotal", Name = "P285_InYearNSENSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P285_InYearNSENSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "27c055f869ca468c81c79ca352406591"), CalculationSpecification(Id = "P286_PriorYearAdjustmentSubtotal", Name = "P286_PriorYearAdjustmentSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P286_PriorYearAdjustmentSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "a4e0d3d8021e4377a2431d292354f357"), CalculationSpecification(Id = "P287_InYearPriorYearAdjsutmentSubtotal", Name = "P287_InYearPriorYearAdjsutmentSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P287_InYearPriorYearAdjsutmentSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "fc52361002854ebd8446a8aa3ac77c86"), CalculationSpecification(Id = "P298_Growth", Name = "P298_Growth"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P298_Growth()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "17e45d0100234b6abbb196a3c7358f99"), CalculationSpecification(Id = "P299_InYearGrowth", Name = "P299_InYearGrowth"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P299_InYearGrowth()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "47d6bdac158c467ba6db179da7f107eb"), CalculationSpecification(Id = "P300_SBSOutcomeAdjustment", Name = "P300_SBSOutcomeAdjustment"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P300_SBSOutcomeAdjustment()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "60baea018b904858bb6f474bdbeee319"), CalculationSpecification(Id = "P301_InYearSBSOutcomeAdjustment", Name = "P301_InYearSBSOutcomeAdjustment"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "41080d8da69046e3a26ff4fb6894eaa8", Name = "Other Factors")]
    public decimal P301_InYearSBSOutcomeAdjustment()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "48b94d0db5884f1fa2473f4b3331523c"), CalculationSpecification(Id = "P120_PPAindicator", Name = "P120_PPAindicator"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P120_PPAindicator()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "a41fd3f52ee349e4919b372073464caa"), CalculationSpecification(Id = "P121_PPAY5to6Proportion73", Name = "P121_PPAY5to6Proportion73"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P121_PPAY5to6Proportion73()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "0cfe98314bb24c228c4e8b3249b99baf"), CalculationSpecification(Id = "P122_PPAY5to6Proportion78", Name = "P122_PPAY5to6Proportion78"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P122_PPAY5to6Proportion78()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e6591de6c22d4b45a0ce648ab582b410"), CalculationSpecification(Id = "P122a_PPAY7378forFAPOnly", Name = "P122a_PPAY7378forFAPOnly"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P122a_PPAY7378forFAPOnly()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "abcc3de778b8420387b9310ae8bbccda"), CalculationSpecification(Id = "P123_PPAY1to4ProportionUnder", Name = "P123_PPAY1to4ProportionUnder"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P123_PPAY1to4ProportionUnder()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f4b6b5b34cfc447b958ecce485ad9f08"), CalculationSpecification(Id = "P124_PPAY5to6NOR", Name = "P124_PPAY5to6NOR"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P124_PPAY5to6NOR()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c2e335610a3c4dd7b3fccd0b955e9a46"), CalculationSpecification(Id = "P125_PPAY1to4NOR", Name = "P125_PPAY1to4NOR"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P125_PPAY1to4NOR()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "9b2e9769e9e24058a31c2ed89b513c18"), CalculationSpecification(Id = "P126_PPAPriNOR", Name = "P126_PPAPriNOR"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P126_PPAPriNOR()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "27e6d2e97ce846afaefe584450b166f3"), CalculationSpecification(Id = "P127_PPARate", Name = "P127_PPARate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P127_PPARate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "a71f5b0253344507982c0647cc2dec71"), CalculationSpecification(Id = "P128_PPAWeighting", Name = "P128_PPAWeighting"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P128_PPAWeighting()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e71a0f607866460ea6615dc3515c19ca"), CalculationSpecification(Id = "P129_PPAPupilsY5to6NotAchieving", Name = "P129_PPAPupilsY5to6NotAchieving"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P129_PPAPupilsY5to6NotAchieving()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "74f3feedf548410ea40f20e8191ef022"), CalculationSpecification(Id = "P130_PPAPupilsY1to4NotAchieving", Name = "P130_PPAPupilsY1to4NotAchieving"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P130_PPAPupilsY1to4NotAchieving()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "0023814b87854fe7ba7d81c3f3fdc630"), CalculationSpecification(Id = "P131_PPATotalPupilsY1to6NotAchieving", Name = "P131_PPATotalPupilsY1to6NotAchieving"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P131_PPATotalPupilsY1to6NotAchieving()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e81c1916df854c4281e02e4d8d518add"), CalculationSpecification(Id = "P132_PPATotalProportionNotAchieving", Name = "P132_PPATotalProportionNotAchieving"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P132_PPATotalProportionNotAchieving()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "eed8c60bfe2c4861ad36f774006c9ccc"), CalculationSpecification(Id = "P133_PPATotalFunding", Name = "P133_PPATotalFunding"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P133_PPATotalFunding()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4b3f788d1dd7453984e945665cefc2a3"), CalculationSpecification(Id = "P134_NSENPPA", Name = "P134_NSENPPA"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P134_NSENPPA()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7922fff3d6f34d2b90e6b50f771cfde9"), CalculationSpecification(Id = "P134a_NSENPPA_Percent", Name = "P134a_NSENPPA_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P134a_NSENPPA_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "ea5abbc1531046d891bee59e356c4347"), CalculationSpecification(Id = "P135_InYearPPASubtotal", Name = "P135_InYearPPASubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P135_InYearPPASubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "0efdc8a3af9c4a2895ca4611ba71c5f7"), CalculationSpecification(Id = "P136_SecPA_Y7Factor", Name = "P136_SecPA_Y7Factor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P136_SecPA_Y7Factor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "2f4e37ee94bd4d6f8cc0d1561397077f"), CalculationSpecification(Id = "P136a_SecPA_Y7NationalWeight", Name = "P136a_SecPA_Y7NationalWeight"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P136a_SecPA_Y7NationalWeight()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d44f98bec9d24895b923e2c04c031704"), CalculationSpecification(Id = "P138_SecPARate", Name = "P138_SecPARate"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P138_SecPARate()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "4b976e67c0a146519796c3d4dfd9fdfc"), CalculationSpecification(Id = "P138a_SecPA_AdjustedSecFactor", Name = "P138a_SecPA_AdjustedSecFactor"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P138a_SecPA_AdjustedSecFactor()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f3134ec4b0e34c4089dac6d48f39ee7b"), CalculationSpecification(Id = "P139_SecPASubtotal", Name = "P139_SecPASubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P139_SecPASubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "981f3cf6d2034b5e8663e7f97b1e829d"), CalculationSpecification(Id = "P140_NSENSecPA", Name = "P140_NSENSecPA"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P140_NSENSecPA()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "79a9b2578fc843d380168f7f4cf48cff"), CalculationSpecification(Id = "P140a_NSENSecPA_Percent", Name = "P140a_NSENSecPA_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P140a_NSENSecPA_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3ec44c9455ee458aaf97eabdb5d2a3eb"), CalculationSpecification(Id = "P141_InYearSecPASubtotal", Name = "P141_InYearSecPASubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "c6b57f57424c494fb0ed394ffb1af34c", Name = "Prior Attainment")]
    public decimal P141_InYearSecPASubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e9126ef86bf34987b5b6d42bc934b198"), CalculationSpecification(Id = "P185a_Phase", Name = "P185a_Phase"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P185a_Phase()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "90c6d38febfa4ea1a80fd526c354c78c"), CalculationSpecification(Id = "P186_SparsityTaperFlagPri", Name = "P186_SparsityTaperFlagPri"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P186_SparsityTaperFlagPri()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "793054bf6c4d47d3b1b0794241a4b484"), CalculationSpecification(Id = "P187_SparsityTaperFlagMid", Name = "P187_SparsityTaperFlagMid"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P187_SparsityTaperFlagMid()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "a46ad8c813a748e5a21c8aaaaab0b78c"), CalculationSpecification(Id = "P188_SparsityTaperFlagSec", Name = "P188_SparsityTaperFlagSec"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P188_SparsityTaperFlagSec()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "66f49017ab8e4cb69103b3de755eca55"), CalculationSpecification(Id = "P189_SparsityTaperFlagAllThru", Name = "P189_SparsityTaperFlagAllThru"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P189_SparsityTaperFlagAllThru()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "bb74cb78e426421891c44bba64959180"), CalculationSpecification(Id = "P190_SparsityUnit", Name = "P190_SparsityUnit"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P190_SparsityUnit()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "aba2fe32cf744d40b8b03c1dd506bd52"), CalculationSpecification(Id = "P191_SparsityDistance", Name = "P191_SparsityDistance"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P191_SparsityDistance()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d9cb6bfce15a4342a8fcca358d1fff65"), CalculationSpecification(Id = "P192_SparsityDistThreshold", Name = "P192_SparsityDistThreshold"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P192_SparsityDistThreshold()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "fc5d1ce5981245daa3d6b95c39b9642c"), CalculationSpecification(Id = "P193_SparsityDistMet_YN", Name = "P193_SparsityDistMet_YN"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P193_SparsityDistMet_YN()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "f65c115ad5234d2f8fb3e21cc34ce8d4"), CalculationSpecification(Id = "P194_SparsityAveYGSize", Name = "P194_SparsityAveYGSize"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P194_SparsityAveYGSize()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "12481e5ad58e4661a11c2f966839c684"), CalculationSpecification(Id = "P195_SparsityYGThreshold", Name = "P195_SparsityYGThreshold"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P195_SparsityYGThreshold()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "eab83558254e4bd8b6585f84af9b3bfc"), CalculationSpecification(Id = "P196_SparsityYGThresholdMet_YN", Name = "P196_SparsityYGThresholdMet_YN"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P196_SparsityYGThresholdMet_YN()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "624f1ff6f39d4475890f7447e501f9c1"), CalculationSpecification(Id = "P197_SparsityLumpSumSubtotal", Name = "P197_SparsityLumpSumSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P197_SparsityLumpSumSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "6eebfeaea6ae45cc818b6d557203f271"), CalculationSpecification(Id = "P198_SparsityTaperSubtotal", Name = "P198_SparsityTaperSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P198_SparsityTaperSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "96a7f3364e87487591a855f8961c2819"), CalculationSpecification(Id = "P198a_SubtotalLump_Taper_For_FAP_Only", Name = "P198a_SubtotalLump_Taper_For_FAP_Only"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P198a_SubtotalLump_Taper_For_FAP_Only()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3502ff97f1ac459ea1cb8ef43e7f4cb1"), CalculationSpecification(Id = "P199_InYearSparsityLumpSumSubtotal", Name = "P199_InYearSparsityLumpSumSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P199_InYearSparsityLumpSumSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "e7ed2e3a382c4b979c6888de961aa238"), CalculationSpecification(Id = "P200_InYearSparsityTaperSubtotal", Name = "P200_InYearSparsityTaperSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P200_InYearSparsityTaperSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d0ccae5f7bff415e91204f3074334f21"), CalculationSpecification(Id = "P200a_InYear_SubtotalLump_Taper_for_FAP_Only", Name = "P200a_InYear_SubtotalLump_Taper_for_FAP_Only"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P200a_InYear_SubtotalLump_Taper_for_FAP_Only()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "aef0f93950884305a3331242d77ed8ed"), CalculationSpecification(Id = "P212_PYG", Name = "P212_PYG"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P212_PYG()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "eded3285fd8a4df0b42066306cff0c2d"), CalculationSpecification(Id = "P213_SYG", Name = "P213_SYG"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P213_SYG()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "0aa1db5f847343f09d2af5b763d2dee8"), CalculationSpecification(Id = "P236_NSENSparsity", Name = "P236_NSENSparsity"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P236_NSENSparsity()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3aa2838fe91b492087f7e8693244c677"), CalculationSpecification(Id = "P236a_NSENSparsity_Percent", Name = "P236a_NSENSparsity_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "4eda722dff5b4f6f9e3d13eae928be8a", Name = "Sparsity")]
    public decimal P236a_NSENSparsity_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "69d34ee7dc7c4893a0ce95638e0153de"), CalculationSpecification(Id = "P249_SplitSiteSubtotal", Name = "P249_SplitSiteSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "6857aab8747c4d699b69bba5858b98a3", Name = "Split Sites")]
    public decimal P249_SplitSiteSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "cf18451e27f34686b5377e4ee9a74099"), CalculationSpecification(Id = "P250_NSENSplitSites", Name = "P250_NSENSplitSites"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "6857aab8747c4d699b69bba5858b98a3", Name = "Split Sites")]
    public decimal P250_NSENSplitSites()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "3a1d41b77a4a4bed8af90e6005eeef62"), CalculationSpecification(Id = "P250a_NSENSplitSites_Percent", Name = "P250a_NSENSplitSites_Percent"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "6857aab8747c4d699b69bba5858b98a3", Name = "Split Sites")]
    public decimal P250a_NSENSplitSites_Percent()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "166669d03a364266888c26c2998e0ac7"), CalculationSpecification(Id = "P251_InYearSplitSitesSubtotal", Name = "P251_InYearSplitSitesSubtotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "6857aab8747c4d699b69bba5858b98a3", Name = "Split Sites")]
    public decimal P251_InYearSplitSitesSubtotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "d367e314dfb448ffac178c3a4f175791"), CalculationSpecification(Id = "P001_1718DaysOpen", Name = "P001_1718DaysOpen"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P001_1718DaysOpen()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "88f2073d9b5645919acddb3c3c1b66de"), CalculationSpecification(Id = "Lump_Sum_Total", Name = "Lump_Sum_Total"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal Lump_Sum_Total()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "40f7a3c6005f40ffaa11e14aa2f082ea"), CalculationSpecification(Id = "InYearLumpSum", Name = "InYearLumpSum"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal InYearLumpSum()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "1873071ca2cc4ae291bc7dc8c4a169de"), CalculationSpecification(Id = "P288_SBSFundingTotal", Name = "P288_SBSFundingTotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P288_SBSFundingTotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "43e7af7b17d949f6810a51d9d349a01b"), CalculationSpecification(Id = "P289_InYearSBSFundingTotal", Name = "P289_InYearSBSFundingTotal"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P289_InYearSBSFundingTotal()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "8ed956c88cf04d8f8c316998432c1f28"), CalculationSpecification(Id = "P290_ISBTotalSBSFunding", Name = "P290_ISBTotalSBSFunding"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P290_ISBTotalSBSFunding()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "cb6dcb7c2a044949b8ab5033d0c88802"), CalculationSpecification(Id = "P291_TotalPupilLedFactors", Name = "P291_TotalPupilLedFactors"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P291_TotalPupilLedFactors()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "df5d5e9bb0164eeea280ed06cc443607"), CalculationSpecification(Id = "P292_InYearTotalPupilLedfactors", Name = "P292_InYearTotalPupilLedfactors"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P292_InYearTotalPupilLedfactors()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "7f779a2fec97464ca24277b361f01581"), CalculationSpecification(Id = "P293_TotalOtherFactors", Name = "P293_TotalOtherFactors"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P293_TotalOtherFactors()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "78a3576fdebd4c55aede3b9391c3e682"), CalculationSpecification(Id = "P293a_TotalOtherFactors_NoExc", Name = "P293a_TotalOtherFactors_NoExc"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P293a_TotalOtherFactors_NoExc()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c7debd75137e449b84f4590ea56300e2"), CalculationSpecification(Id = "P294_InYearTotalOtherFactors", Name = "P294_InYearTotalOtherFactors"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P294_InYearTotalOtherFactors()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "6e09ad89fcbe4d889b65da4f2792cdc6"), CalculationSpecification(Id = "P294a_InYearTotalOtherFactors_NoExc", Name = "P294a_InYearTotalOtherFactors_NoExc"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P294a_InYearTotalOtherFactors_NoExc()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c30d18ef6d1847b69848e7ba8da04a1a"), CalculationSpecification(Id = "P295_Dedelegation", Name = "P295_Dedelegation"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P295_Dedelegation()
    {
        return decimal.MinValue;
    }

    [Calculation(Id = "c60a66975cfc41bead2b6b6d33c2e3b9"), CalculationSpecification(Id = "P296_InYearDedelegation", Name = "P296_InYearDedelegation"), PolicySpecification(Id = "93f568b56656481ab43ac14119890c7f", Name = "School Budget Share"), PolicySpecification(Id = "b2e46eab20374cd1864365a94a7ab9b3", Name = "Totals")]
    public decimal P296_InYearDedelegation()
    {
        return decimal.MinValue;
    }
}