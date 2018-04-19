namespace NinMemApi.Data.Elements
{
    public static class VL
    {
        public static int NA = nameof(NA).GetHashCode();
        public static int NAT = nameof(NAT).GetHashCode();
        public static int DV = nameof(DV).GetHashCode();
        public static int EV = nameof(EV).GetHashCode();
        public static int RC = nameof(RC).GetHashCode();
        public static int RT = nameof(RT).GetHashCode();
        public static int RAU = nameof(RAU).GetHashCode();
        public static int AA = nameof(AA).GetHashCode();
        public static int M = nameof(M).GetHashCode();
        public static int CA = nameof(CA).GetHashCode();
        public static int CAC = nameof(CAC).GetHashCode();
        public static int T = nameof(T).GetHashCode();
        public static int BC = nameof(BC).GetHashCode();
        public static int TN = nameof(TN).GetHashCode();
        public static int AEMS = nameof(AEMS).GetHashCode();
        public static int AEPD = nameof(AEPD).GetHashCode();
        public static int AESD = nameof(AESD).GetHashCode();
        public static int AESS = nameof(AESS).GetHashCode();
        public static int AET = nameof(AET).GetHashCode();
        public static int AETL = nameof(AETL).GetHashCode();
    }

    public static class VertexLabels
    {
        public static int NatureArea => VL.NA;
        public static int NatureAreaType => VL.NAT;
        public static int DescriptionVariable => VL.DV;
        public static int EnvironmentVariable => VL.EV;
        public static int RedlistCategory => VL.RC;
        public static int RedlistTheme => VL.RT;
        public static int RedlistAssessmentUnit => VL.RAU;
        public static int AdministrativeArea => VL.AA;
        public static int Municipality => VL.M;
        public static int ConservationArea => VL.CA;
        public static int ConservationAreaCategory => VL.CAC;
        public static int Taxon => VL.T;
        public static int BlacklistCategory => VL.BC;
        public static int TreeNode => VL.TN;
        public static int MatingSystem => VL.AEMS;
        public static int PrimaryDiet => VL.AEPD;
        public static int SexualDimorphism => VL.AESD;
        public static int SocialSystem => VL.AESS;
        public static int Terrestriality => VL.AET;
        public static int TrophicLevel => VL.AETL;
    }
}