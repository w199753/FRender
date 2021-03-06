using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using frp;
public static class Utility 
{
    public static List<V> GetValueList<K,V>(this System.Collections.Generic.Dictionary<K, V>.ValueCollection collection)
    {
        List<V> valueList = new List<V>();
        foreach(var value in collection)
        {
            valueList.Add(value);
        }
        return valueList;
    }
}

public static class MaterialPool
{
    private static Dictionary<string,Material> m_Dic = new Dictionary<string, Material>(8); 
    public static Material GetMaterial(string name)
    {
            if (!m_Dic.ContainsKey(name) || !m_Dic[name])
                m_Dic[name] = new Material(Shader.Find(name));
            return m_Dic[name];
    }
}


public class PermutedMatrix
{
    public PermutedMatrix(Matrix4x4 m)
    {
        mMat44 = m;
    }

    static int permute(int v)
    {
        if (v == 1)
            return 0;
        if (v == -1)
            return 1;
        if (v == 0)
            return 2;
        return 0;
    }

    public float GetByMN(int m, int n)
    {
        int row = permute(m);
        int column = permute(n);

        if (row == 0 && column == 0)
            return mMat44.m00;
        if (row == 0 && column == 1)
            return mMat44.m01;
        if (row == 0 && column == 2)
            return mMat44.m02;

        if (row == 1 && column == 0)
            return mMat44.m10;
        if (row == 1 && column == 1)
            return mMat44.m11;
        if (row == 1 && column == 2)
            return mMat44.m12;

        if (row == 2 && column == 0)
            return mMat44.m20;
        if (row == 2 && column == 1)
            return mMat44.m21;
        if (row == 2 && column == 2)
            return mMat44.m22;

        return -1;
    }

    private Matrix4x4 mMat44;
}
public class SHRotate
{

    private static float delta(int m, int n)
    {
        return (m == n ? 1 : 0);
    }

    private static void uvw(int l, int m, int n, ref float u, ref float v, ref float w)
    {
        float d = delta(m, 0);
        int abs_m = Mathf.Abs(m);

        float denom;
        if (Mathf.Abs(n) == l)
            denom = (2 * l) * (2 * l - 1);

        else
            denom = (l + n) * (l - n);

        u = Mathf.Sqrt((l + m) * (l - m) / denom);
        v = 0.5f * Mathf.Sqrt((1 + d) * (l + abs_m - 1) * (l + abs_m) / denom) * (1 - 2 * d);
        w = -0.5f * Mathf.Sqrt((l - abs_m - 1) * (l - abs_m) / denom) * (1 - d);
    }

    private static float P(int i, int l, int a, int b, PermutedMatrix R, SHRotateMatrix M)
    {
        if (b == -l)
        {
            return (R.GetByMN(i, 1) * M.GetValueByBand(l - 1, a, -l + 1) + R.GetByMN(i, -1) * M.GetValueByBand(l - 1, a, l - 1));
        }
        else if (b == l)
        {
            return (R.GetByMN(i, 1) * M.GetValueByBand(l - 1, a, l - 1) - R.GetByMN(i, -1) * M.GetValueByBand(l - 1, a, -l + 1));
        }
        else
        {
            return (R.GetByMN(i, 0) * M.GetValueByBand(l - 1, a, b));
        }
    }

    private static float U(int l, int m, int n, PermutedMatrix R, SHRotateMatrix M)
    {
        if (m == 0)
            return (P(0, l, 0, n, R, M));

        return (P(0, l, m, n, R, M));
    }


    private static float V(int l, int m, int n, PermutedMatrix R, SHRotateMatrix M)
    {
        if (m == 0)
        {
            float p0 = P(1, l, 1, n, R, M);
            float p1 = P(-1, l, -1, n, R, M);
            return (p0 + p1);
        }
        else if (m > 0)
        {
            float d = delta(m, 1);
            float p0 = P(1, l, m - 1, n, R, M);
            float p1 = P(-1, l, -m + 1, n, R, M);
            return (p0 * Mathf.Sqrt(1 + d) - p1 * (1 - d));
        }
        else
        {
            float d = delta(m, -1);
            float p0 = P(1, l, m + 1, n, R, M);
            float p1 = P(-1, l, -m - 1, n, R, M);
            return (p0 * (1 - d) + p1 * Mathf.Sqrt(1 - d));
        }
    }


    private static float W(int l, int m, int n, PermutedMatrix R, SHRotateMatrix M)
    {
        if (m == 0)
        {
            return (0);
        }
        else if (m > 0)
        {
            float p0 = P(1, l, m + 1, n, R, M);
            float p1 = P(-1, l, -m - 1, n, R, M);
            return (p0 + p1);
        }
        else // m < 0
        {
            float p0 = P(1, l, m - 1, n, R, M);
            float p1 = P(-1, l, -m + 1, n, R, M);
            return (p0 - p1);
        }
    }


    private static float M(int l, int m, int n, PermutedMatrix R, SHRotateMatrix M)
    {
        // First get the scalars
        float u = 0.0f, v = 0.0f, w = 0.0f;
        uvw(l, m, n, ref u, ref v, ref w);

        // Scale by their functions
        if (u != 0.0f)
            u *= U(l, m, n, R, M);
        if (v != 0.0f)
            v *= V(l, m, n, R, M);
        if (w != 0.0f)
            w *= W(l, m, n, R, M);

        return (u + v + w);
    }


    public static Vector3[] Rotate(Vector3[] src, Matrix4x4 rot)
    {
        SHRotateMatrix shrm = transfer(rot, (int)Mathf.Sqrt(src.Length));
        Vector3[] dest = shrm.Transform(src);
        return dest;
    }

    public static SHRotateMatrix transfer(Matrix4x4 rot, int bands)
    {
        SHRotateMatrix result = new SHRotateMatrix(bands * bands);
        result.SetValue(0, 0, 1);

        PermutedMatrix pm = new PermutedMatrix(rot);

        for (int m = -1; m <= 1; m++)
            for (int n = -1; n <= 1; n++)
                result.SetValueByBand(1, m, n, pm.GetByMN(m, n));

        for (int band = 2; band < bands; band++)
        {
            for (int m = -band; m <= band; m++)
                for (int n = -band; n <= band; n++)
                    result.SetValueByBand(band, m, n, M(band, m, n, pm, result));
        }

        return result;
    }

}

public class SHRotateMatrix
{
    public Vector3[] Transform(Vector3[] src)
    {
        int bands = (int)Mathf.Sqrt(mDim);
        Vector3[] dest = new Vector3[src.Length];
        for (int i = 0; i < dest.Length; i++)
            dest[i] = Vector3.zero;
        for (int l = 0; l < bands; l++)
        {
            for (int mo = -l; mo <= l; mo++)
            {
                int outputIndex = GetIndexByLM(l, mo);
                Vector3 target = Vector3.zero;
                for (int mi = -l; mi <= l; mi++)
                {
                    int inputIndex = GetIndexByLM(l, mi);
                    float matValue = GetValueByBand(l, mo, mi);
                    Vector3 source = src[inputIndex];
                    target += source * matValue;
                }

                dest[outputIndex] = target;
            }
        }

        return dest;
    }

    public SHRotateMatrix(int dim)
    {
        mDim = dim;
        mMatrix = new float[mDim][];
        for (int i = 0; i < mDim; i++)
        {
            mMatrix[i] = new float[mDim];
            for (int j = 0; j < mDim; j++)
            {
                mMatrix[i][j] = 0.0f;
            }
        }
    }

    public void SetValue(int i, int j, float value)
    {
        mMatrix[i][j] = value;
    }

    public float GetValueByBand(int l, int a, int b)
    {
        int centre = (l + 1) * l;
        return mMatrix[centre + a][centre + b];
    }

    public void SetValueByBand(int l, int a, int b, float value)
    {
        int centre = (l + 1) * l;
        mMatrix[centre + a][centre + b] = value;
    }

    private int GetIndexByLM(int l, int m)
    {
        return (l + 1) * l + m;
    }

    public int mDim;
    private float[][] mMatrix;
}




public static class StaticData
    {
        public static Vector3[] dirs = {
            new Vector3(-0.6522197f,0.7566256f,0.04612207f),
            new Vector3(0.7762083f,-0.475227f,-0.4143189f),
            new Vector3(0.8057191f,0.1056374f,-0.5828013f),
            new Vector3(-0.8448897f,0.08550198f,0.5280632f),
            new Vector3(0.7860208f,0.6104082f,-0.09784205f),
            new Vector3(0.0823328f,-0.03405384f,-0.9960229f),
            new Vector3(0.3534262f,0.4258367f,-0.8329184f),
            new Vector3(0.5913832f,0.2338144f,0.7717493f),
            new Vector3(0.2196933f,-0.7650623f,-0.6053218f),
            new Vector3(0.750181f,0.2767342f,0.6005387f),
            new Vector3(-0.6484057f,0.7601258f,0.04217674f),
            new Vector3(0.1984718f,-0.8863804f,-0.4182566f),
            new Vector3(-0.3612449f,-0.6325378f,-0.6851263f),
            new Vector3(0.02594085f,0.9821016f,0.1865572f),
            new Vector3(-0.1960014f,0.1931933f,-0.9613844f),
            new Vector3(-0.3433865f,0.7302604f,-0.5905974f),
            new Vector3(0.8977489f,-0.02955557f,-0.4395148f),
            new Vector3(0.586249f,-0.8100457f,-0.01175047f),
            new Vector3(-0.3144649f,0.8206455f,-0.4771295f),
            new Vector3(0.31621f,0.7445439f,0.5879334f),
            new Vector3(-0.9710144f,0.04770843f,-0.2342115f),
            new Vector3(0.5589296f,-0.6827868f,-0.4705316f),
            new Vector3(0.915587f,-0.3189737f,-0.2448595f),
            new Vector3(0.003755379f,-0.999122f,-0.04172521f),
            new Vector3(0.174852f,0.3356467f,-0.9256176f),
            new Vector3(0.4723623f,0.7783638f,-0.41355f),
            new Vector3(0.003477569f,-0.5612205f,-0.827659f),
            new Vector3(0.1834771f,-0.07794915f,0.9799286f),
            new Vector3(-0.7060484f,0.2985547f,-0.6421532f),
            new Vector3(-0.8337251f,-0.5100946f,-0.2114378f),
            new Vector3(0.04385382f,0.6313244f,-0.774278f),
            new Vector3(-0.9137032f,-0.2876615f,0.2870494f),
            new Vector3(0.1108165f,0.975517f,-0.1899642f),
            new Vector3(0.1181937f,0.9771344f,0.1767445f),
            new Vector3(0.8851486f,0.3972199f,-0.2423394f),
            new Vector3(-0.4757161f,0.3873368f,-0.7897243f),
            new Vector3(-0.2998533f,0.9227391f,-0.2421579f),
            new Vector3(0.9716226f,0.2158228f,0.09679952f),
            new Vector3(0.6034677f,0.7845354f,0.1425868f),
            new Vector3(-0.3847187f,-0.7257406f,-0.5703439f),
            new Vector3(0.9145527f,-0.2637542f,0.3066383f),
            new Vector3(0.03721754f,-0.8129897f,-0.5810875f),
            new Vector3(0.3293082f,0.7529441f,-0.5697643f),
            new Vector3(-0.9204749f,-0.3203817f,-0.2237888f),
            new Vector3(-0.5022533f,0.4854466f,-0.7155998f),
            new Vector3(0.1330288f,0.3739693f,0.9178509f),
            new Vector3(-0.2932482f,0.9559114f,0.01545555f),
            new Vector3(0.6149308f,-0.1125846f,0.7805029f),
            new Vector3(0.9563096f,0.2733866f,-0.1035946f),
            new Vector3(0.6355255f,0.2490746f,-0.7308004f),
            new Vector3(-0.2982728f,0.8936174f,-0.3353823f),
            new Vector3(-0.5930429f,0.167044f,0.7876525f),
            new Vector3(-0.4741956f,0.3356857f,0.8139126f),
            new Vector3(-0.4489293f,-0.7272727f,0.5191694f),
            new Vector3(0.4582474f,-0.7647683f,-0.4529225f),
            new Vector3(-0.7662893f,-0.4303068f,-0.477113f),
            new Vector3(-0.2043651f,-0.9432766f,0.2616566f),
            new Vector3(0.3846089f,-0.9229466f,-0.0156746f),
            new Vector3(0.81489f,0.4096742f,0.4100261f),
            new Vector3(0.9922818f,0.108011f,-0.06091421f),
            new Vector3(-0.6661797f,0.4774081f,0.5729625f),
            new Vector3(0.05864267f,-0.9938797f,0.09361778f),
            new Vector3(0.3677514f,0.678628f,-0.6357853f),
            new Vector3(-0.755185f,0.6523973f,0.06382337f),
            new Vector3(-0.8027484f,-0.4845304f,0.3475991f),
            new Vector3(-0.7108428f,-0.6292278f,-0.3142847f),
            new Vector3(-0.5710878f,-0.5924519f,0.5682073f),
            new Vector3(-0.2921706f,-0.8207957f,-0.4908471f),
            new Vector3(-0.3136744f,0.9192001f,-0.2380745f),
            new Vector3(-0.1167585f,-0.7512578f,-0.6495991f),
            new Vector3(-0.2734743f,0.7664171f,0.5812199f),
            new Vector3(-0.01732842f,0.3287416f,-0.9442609f),
            new Vector3(0.2376671f,0.03477187f,-0.9707241f),
            new Vector3(-0.1325362f,0.8220522f,-0.5537727f),
            new Vector3(0.6849838f,-0.006298794f,-0.7285311f),
            new Vector3(0.5344512f,-0.202759f,-0.8205185f),
            new Vector3(-0.215411f,0.44409f,-0.8697024f),
            new Vector3(-0.222073f,0.6637417f,-0.7142342f),
            new Vector3(0.4043152f,0.7030191f,-0.5850585f),
            new Vector3(-0.5523478f,-0.06713034f,0.8309064f),
            new Vector3(0.68051f,0.2790411f,0.6775265f),
            new Vector3(0.9244658f,-0.364723f,0.111086f),
            new Vector3(-0.07359709f,-0.8715178f,-0.4848094f),
            new Vector3(0.5269035f,-0.4472594f,0.7227253f),
            new Vector3(0.9383007f,-0.3174796f,0.1371076f),
            new Vector3(-0.2919534f,-0.6289269f,0.7205652f),
            new Vector3(0.3541999f,-0.4009984f,-0.844833f),
            new Vector3(-0.6289085f,-0.1027098f,-0.7706652f),
            new Vector3(-0.3488514f,-0.8856148f,-0.3065765f),
            new Vector3(-0.5671126f,0.05110786f,0.8220531f),
            new Vector3(-0.5190961f,-0.6657813f,0.5359799f),
            new Vector3(0.9495216f,0.3047331f,0.07447509f),
            new Vector3(0.2539687f,0.8509044f,0.4598494f),
            new Vector3(-0.1885955f,-0.9710129f,0.1468523f),
            new Vector3(-0.9688188f,0.215296f,0.1226287f),
            new Vector3(-0.1574265f,-0.9262946f,-0.3423378f),
            new Vector3(0.9442139f,0.1938305f,0.2662516f),
            new Vector3(-0.9868023f,-0.1321156f,0.09363095f),
            new Vector3(-0.2743124f,-0.9096587f,-0.3118875f),
            new Vector3(-0.6548627f,0.7435272f,0.1353593f),
            new Vector3(-0.6548384f,0.7527861f,0.06707932f),
            new Vector3(0.1762521f,-0.892959f,-0.4141974f),
            new Vector3(0.5377529f,0.770443f,0.3424024f),
            new Vector3(0.1401239f,-0.02307531f,0.989865f),
            new Vector3(0.3544381f,-0.2769804f,-0.8931156f),
            new Vector3(0.8679804f,-0.06372744f,0.4924926f),
            new Vector3(-0.2447903f,-0.4200295f,0.8738724f),
            new Vector3(-0.1176424f,0.9750055f,-0.1884796f),
            new Vector3(0.5662721f,0.2828393f,0.7741691f),
            new Vector3(-0.1028589f,0.9126015f,0.3957003f),
            new Vector3(0.6386537f,-0.08620979f,0.7646499f),
            new Vector3(-0.6471962f,0.2787287f,-0.7095402f),
            new Vector3(-0.1238312f,-0.8400008f,-0.5282654f),
            new Vector3(-0.005331195f,-0.882149f,-0.4709403f),
            new Vector3(-0.3880488f,0.06905588f,-0.9190481f),
            new Vector3(-0.1850565f,0.07264211f,-0.9800395f),
            new Vector3(-0.8654213f,-0.4973421f,-0.06080174f),
            new Vector3(0.9306118f,-0.1790095f,0.3192448f),
            new Vector3(0.5775619f,0.09076601f,-0.8112853f),
            new Vector3(0.5220122f,0.7863732f,-0.3303338f),
            new Vector3(0.5962259f,0.4772299f,-0.6455744f),
            new Vector3(-0.5409412f,0.8038846f,-0.24729f),
            new Vector3(-0.926775f,-0.05261159f,0.3719139f),
            new Vector3(0.3984074f,-0.8014261f,0.4460804f),
            new Vector3(-0.6944745f,0.3392373f,-0.634526f),
            new Vector3(0.08153568f,0.9754043f,-0.2047886f),
            new Vector3(-0.7064635f,-0.7010754f,-0.09696753f),
            new Vector3(-0.6582159f,0.268108f,-0.7034699f),
            new Vector3(0.8021378f,0.5942292f,-0.05887838f),
            new Vector3(0.4555468f,0.5869333f,0.6693177f),
            new Vector3(-0.4699337f,0.8606308f,-0.1961556f),
            new Vector3(-0.2469125f,0.9249086f,0.289099f),
            new Vector3(-0.7561041f,-0.4461185f,-0.478837f),
            new Vector3(0.9886385f,-0.1253684f,-0.08292492f),
            new Vector3(0.5984837f,-0.6808106f,-0.4222727f),
            new Vector3(-0.296531f,0.4045886f,0.865088f),
            new Vector3(0.3456123f,-0.7412718f,-0.5753853f),
            new Vector3(-0.48368f,-0.8751069f,-0.01554129f),
            new Vector3(0.9074942f,0.1432101f,-0.3948991f),
            new Vector3(-0.7644781f,-0.1683743f,0.6222727f),
            new Vector3(-0.1133668f,-0.9853725f,-0.127236f),
            new Vector3(0.7002991f,-0.09545921f,0.7074381f),
            new Vector3(-0.5501685f,-0.6107216f,0.5695032f),
            new Vector3(-0.2357749f,0.942475f,0.236962f),
            new Vector3(-0.1623711f,-0.6917355f,0.7036601f),
            new Vector3(0.5654924f,0.1855206f,-0.8036171f),
            new Vector3(0.5025438f,0.3402864f,-0.7947671f),
            new Vector3(0.6629681f,0.1557162f,0.7322744f),
            new Vector3(0.6696701f,0.6711755f,0.3179079f),
            new Vector3(0.6814085f,0.4116539f,0.6051641f),
            new Vector3(-0.8323607f,-0.458143f,0.3118986f),
            new Vector3(0.2929738f,0.9561108f,-0.004299785f),
            new Vector3(-0.1953862f,0.4371576f,0.8779051f),
            new Vector3(-0.5214908f,0.2972572f,-0.7998034f),
            new Vector3(-0.5952085f,-0.001618744f,-0.8035696f),
            new Vector3(0.562941f,0.6657109f,-0.4898229f),
            new Vector3(-0.2903609f,-0.4209562f,-0.8593523f),
            new Vector3(0.1082158f,0.8826388f,0.4574255f),
            new Vector3(-0.8245922f,-0.5240623f,-0.2130879f),
            new Vector3(-0.02499896f,0.2401299f,-0.9704188f),
            new Vector3(0.8691913f,0.3398183f,0.3592076f),
            new Vector3(0.4234964f,0.8831585f,-0.2016973f),
            new Vector3(0.5612978f,0.2295637f,0.7951386f),
            new Vector3(0.7004291f,-0.710529f,0.06743554f),
            new Vector3(-0.197401f,0.9231976f,-0.3297561f),
            new Vector3(0.1226893f,0.8984132f,0.4216646f),
            new Vector3(0.4820349f,-0.1591941f,0.8615682f),
            new Vector3(0.3981661f,-0.2421592f,-0.8847726f),
            new Vector3(-0.9727973f,-0.04248755f,0.2277283f),
            new Vector3(0.2990507f,0.9532548f,-0.04328977f),
            new Vector3(-0.9077175f,-0.3731403f,0.191873f),
            new Vector3(-0.6016839f,-0.2812456f,-0.747581f),
            new Vector3(-0.9025404f,-0.3946109f,0.1723459f),
            new Vector3(-0.4286544f,-0.5683988f,-0.7022665f),
            new Vector3(0.4598117f,0.8130106f,-0.3571934f),
            new Vector3(-0.2243772f,-0.6028005f,-0.7656935f),
            new Vector3(-0.1443668f,-0.6516116f,-0.7446883f),
            new Vector3(0.2939805f,-0.909169f,-0.2949362f),
            new Vector3(0.09711637f,-0.3416418f,0.9347991f),
            new Vector3(0.7274919f,0.5427522f,-0.4197327f),
            new Vector3(-0.6367098f,0.2158543f,0.7402753f),
            new Vector3(0.6019094f,0.5855609f,0.5429766f),
            new Vector3(0.8750303f,0.4314575f,-0.2194686f),
            new Vector3(-0.8642756f,0.1638549f,-0.4755831f),
            new Vector3(-0.03112696f,-0.3613941f,0.9318934f),
            new Vector3(-0.2596689f,-0.4841626f,0.8355589f),
            new Vector3(0.7955282f,-0.07182845f,-0.601644f),
            new Vector3(0.4916899f,0.02212133f,-0.8704894f),
            new Vector3(-0.551715f,0.002618981f,-0.8340286f),
            new Vector3(0.4775759f,0.2304161f,0.8478383f),
            new Vector3(0.1612281f,0.9686016f,0.1892524f),
            new Vector3(0.2398911f,0.9217709f,0.3046158f),
            new Vector3(-0.2525558f,0.32949f,0.9097538f),
            new Vector3(-0.6260532f,0.7776684f,0.05735293f),
            new Vector3(0.6459001f,-0.5834863f,-0.4922974f),
            new Vector3(0.07141996f,-0.9489061f,-0.30737f),
            new Vector3(-0.02566419f,0.2485439f,-0.9682806f),
            new Vector3(0.8506654f,-0.008183406f,0.5256439f),
            new Vector3(-0.8832909f,0.4617325f,-0.08124159f),
            new Vector3(-0.2937011f,-0.03386924f,-0.9552971f),
            new Vector3(0.915274f,-0.3571474f,0.1863309f),
            new Vector3(-0.454514f,0.7060943f,0.542999f),
            new Vector3(-0.5468427f,0.3162819f,-0.775196f),
            new Vector3(-0.2195503f,-0.91812f,-0.3299292f),
            new Vector3(-0.1486917f,0.9830123f,-0.1075995f),
            new Vector3(0.7203655f,0.6181619f,-0.3145621f),
            new Vector3(0.6934257f,-0.1344135f,0.7078798f),
            new Vector3(0.8494855f,0.006177188f,-0.5275758f),
            new Vector3(-0.04677472f,-0.3447427f,0.9375311f),
            new Vector3(-0.108075f,-0.2504999f,0.9620653f),
            new Vector3(-0.766616f,0.6228687f,0.1559949f),
            new Vector3(0.7113203f,0.4458674f,-0.5433466f),
            new Vector3(0.7605521f,0.6115847f,-0.2180015f),
            new Vector3(0.09966855f,-0.3035952f,0.9475738f),
            new Vector3(-0.3633217f,0.7396079f,0.5665488f),
            new Vector3(-0.5633715f,0.6957664f,0.4455577f),
            new Vector3(0.04445264f,-0.8910222f,0.451778f),
            new Vector3(0.2441406f,-0.575125f,0.7807859f),
            new Vector3(-0.4983536f,-0.8279904f,0.257052f),
            new Vector3(-0.1916035f,-0.2245675f,-0.9554358f),
            new Vector3(0.2489032f,-0.9384469f,0.2395091f),
            new Vector3(0.8030515f,-0.2787977f,-0.5266689f),
            new Vector3(0.8553757f,0.4974204f,-0.1445867f),
            new Vector3(-0.6239606f,-0.4837792f,-0.6137025f),
            new Vector3(0.1810891f,0.8846446f,-0.4296634f),
            new Vector3(0.3817053f,-0.6964129f,-0.607709f),
            new Vector3(-0.2981562f,-0.4810625f,0.8244281f),
            new Vector3(-0.4528754f,-0.8890225f,-0.06740043f),
            new Vector3(-0.7218359f,-0.1932594f,0.6645328f),
            new Vector3(-0.7302716f,-0.679697f,-0.06866805f),
            new Vector3(0.1901464f,0.8146464f,0.547901f),
            new Vector3(0.07938351f,-0.1247748f,-0.9890043f),
            new Vector3(-0.08779518f,0.982483f,-0.1643749f),
            new Vector3(0.3122891f,0.9142222f,-0.2582117f),
            new Vector3(-0.8452324f,0.4698684f,-0.2545699f),
            new Vector3(-0.4885573f,0.8543994f,0.1769559f),
            new Vector3(-0.5504082f,-0.8341705f,0.03478985f),
            new Vector3(-0.9693398f,0.04032424f,0.2423926f),
            new Vector3(-0.1922845f,-0.7341244f,-0.6512204f),
            new Vector3(-0.2017833f,0.9530699f,0.2257018f),
            new Vector3(0.5602635f,0.7696248f,-0.3062394f),
            new Vector3(0.754932f,-0.6418136f,-0.1347326f),
            new Vector3(-0.6990027f,-0.06689747f,-0.7119831f),
            new Vector3(-0.9259139f,-0.3316721f,-0.180768f),
            new Vector3(-0.5426816f,-0.8205125f,0.1795994f),
            new Vector3(-0.6649708f,0.7134914f,-0.2207798f),
            new Vector3(0.2251844f,-0.7684315f,-0.5990033f),
            new Vector3(-0.01603152f,0.9687501f,0.2475204f),
            new Vector3(-0.002482271f,0.9910324f,-0.1335987f),
            new Vector3(0.5256787f,-0.4516837f,0.7208632f),
            new Vector3(0.515399f,0.3322339f,0.7899269f),
            new Vector3(0.05450846f,-0.9979608f,0.03321411f),
            new Vector3(-0.77031f,0.492663f,-0.4048524f),
            new Vector3(-0.1011144f,-0.9801938f,-0.1702818f),
            new Vector3(0.6043516f,-0.4799482f,-0.6359314f),
            new Vector3(-0.4177084f,0.1259062f,-0.8998151f),
            new Vector3(0.04342759f,0.7513154f,-0.6585128f),
            new Vector3(0.9352f,0.2819127f,0.2143041f),
            new Vector3(-0.9489436f,-0.1528908f,-0.2759175f),
            new Vector3(0.9630877f,-0.07114672f,0.2596157f),
            new Vector3(-0.8570499f,0.5106677f,0.06843901f),
            new Vector3(-0.6705891f,0.07917381f,-0.7375918f),
            new Vector3(-0.6347975f,-0.03583525f,-0.7718471f),
            new Vector3(-0.3302349f,-0.8664281f,-0.374496f),
            new Vector3(0.1579098f,-0.4777652f,-0.8641788f),
            new Vector3(0.248137f,-0.7517003f,0.6110439f),
            new Vector3(0.2863764f,0.9540885f,0.08777031f),
            new Vector3(0.5321466f,0.8419789f,-0.08883475f),
            new Vector3(0.09421863f,-0.2470714f,0.9644058f),
            new Vector3(0.0874975f,0.2589467f,-0.9619203f),
            new Vector3(0.9969755f,-0.03940694f,0.06698576f),
            new Vector3(-0.4402348f,0.2437361f,-0.8641678f),
            new Vector3(0.7845563f,0.2428389f,-0.5705266f),
            new Vector3(-0.8769823f,0.2288155f,-0.4225465f),
            new Vector3(0.306617f,0.5882453f,0.7483004f),
            new Vector3(0.7629681f,-0.0260463f,-0.6459112f),
            new Vector3(-0.1645343f,-0.4497585f,-0.8778644f),
            new Vector3(0.7141829f,0.1895429f,0.6738073f),
            new Vector3(-0.2680808f,0.8912649f,-0.3657589f),
            new Vector3(-0.2567674f,0.9290586f,0.2663095f),
            new Vector3(-0.7154726f,-0.6209701f,0.3201485f),
            new Vector3(-0.6647379f,-0.5406886f,0.5155379f),
            new Vector3(-0.5021108f,0.4381609f,0.7455868f),
            new Vector3(0.5810179f,0.5641836f,0.5866132f),
            new Vector3(-0.08391927f,0.1724101f,0.981444f),
            new Vector3(0.9547735f,0.2968584f,0.01681174f),
            new Vector3(0.09620444f,-0.3598268f,-0.928046f),
            new Vector3(0.6010189f,-0.7900407f,0.12088f),
            new Vector3(0.3287271f,-0.05087257f,0.9430538f),
            new Vector3(-0.3340305f,-0.7624849f,0.5541123f),
            new Vector3(-0.1538719f,-0.5308923f,0.8333527f),
            new Vector3(-0.5020172f,-0.8432884f,0.1919467f),
            new Vector3(-0.124561f,0.8664312f,0.4835097f),
            new Vector3(-0.9753416f,-0.2087181f,-0.07173287f),
            new Vector3(0.676989f,-0.7359816f,0.004124577f),
            new Vector3(-0.09232416f,0.891838f,-0.4428332f),
            new Vector3(-0.7903393f,0.007701336f,-0.6126211f),
            new Vector3(0.4369086f,-0.5977719f,0.6721455f),
            new Vector3(-0.2722376f,0.6875449f,0.6731781f),
            new Vector3(0.593548f,-0.2551372f,-0.7632862f),
            new Vector3(-0.1927889f,-0.7182432f,-0.66855f),
            new Vector3(0.3939547f,-0.1234033f,-0.9108081f),
            new Vector3(-0.4922817f,0.7673033f,-0.4109797f),
            new Vector3(0.0323249f,-0.8819361f,-0.4702592f),
            new Vector3(-0.3073117f,0.340593f,0.8885696f),
            new Vector3(-0.7379971f,0.6181372f,-0.2706784f),
            new Vector3(-0.7388195f,-0.1147558f,-0.6640609f),
            new Vector3(0.3656823f,0.7992397f,-0.4769617f),
            new Vector3(0.03757779f,0.5939739f,-0.8036062f),
            new Vector3(-0.06098426f,-0.93014f,0.3621056f),
            new Vector3(-0.4963845f,0.3351515f,0.800797f),
            new Vector3(-0.8895475f,0.3301522f,0.3157607f),
            new Vector3(0.5541719f,-0.8322988f,-0.01312208f),
            new Vector3(0.02399255f,0.09866369f,-0.9948316f),
            new Vector3(-0.620124f,-0.1525319f,0.7695324f),
            new Vector3(-0.911682f,-0.4107728f,0.0100776f),
            new Vector3(-0.9970268f,-0.06533419f,-0.04085385f),
            new Vector3(0.08478609f,-0.6239581f,0.7768447f),
            new Vector3(-0.9407962f,0.2513137f,-0.2274728f),
            new Vector3(0.386081f,-0.2660933f,-0.883253f),
            new Vector3(0.9969585f,0.07725793f,-0.0102453f),
            new Vector3(0.4898081f,-0.7700976f,-0.4087026f),
            new Vector3(0.06367461f,-0.4261651f,0.9024017f),
            new Vector3(-0.2178314f,0.9234407f,-0.3159221f),
            new Vector3(-0.04998212f,0.9014596f,-0.4299678f),
            new Vector3(-0.03592066f,0.5978036f,0.8008374f),
            new Vector3(0.3678282f,0.3993714f,-0.8397648f),
            new Vector3(-0.5311102f,-0.8175267f,-0.2226478f),
            new Vector3(-0.7176846f,-0.1357955f,0.6829995f),
            new Vector3(-0.8729306f,0.4287144f,0.2328008f),
            new Vector3(-0.4949398f,-0.0158693f,0.8687823f),
            new Vector3(-0.6418546f,0.7529953f,-0.1449855f),
            new Vector3(-0.4187456f,-0.7302351f,0.539823f),
            new Vector3(-0.1775822f,-0.9597287f,0.217682f),
            new Vector3(-0.5365736f,-0.07161738f,-0.840809f),
            new Vector3(0.7705341f,-0.02970046f,0.6367064f),
            new Vector3(-0.8450158f,0.4673199f,-0.2599237f),
            new Vector3(0.01914855f,0.9997149f,0.01426012f),
            new Vector3(0.2688939f,0.2022443f,0.9416971f),
            new Vector3(-0.4525673f,-0.5771222f,-0.6797888f),
            new Vector3(-0.6134294f,-0.1772129f,0.7696102f),
            new Vector3(0.9516745f,0.04209106f,0.3042106f),
            new Vector3(0.5016848f,0.5818837f,-0.6400967f),
            new Vector3(-0.3646319f,-0.361471f,0.8581271f),
            new Vector3(0.7462766f,0.329683f,-0.5782563f),
            new Vector3(-0.4368714f,0.005339681f,-0.8995081f),
            new Vector3(0.3083513f,-0.7577136f,0.5751432f),
            new Vector3(-0.4661391f,-0.1382252f,-0.8738467f),
            new Vector3(-0.4769967f,-0.5823196f,0.6583145f),
            new Vector3(-0.5140913f,-0.5278105f,0.6761111f),
            new Vector3(-0.4550579f,0.5771164f,-0.6781291f),
            new Vector3(0.1693828f,-0.4563476f,-0.873531f),
            new Vector3(0.5419568f,-0.2259549f,-0.8094609f),
            new Vector3(0.09159067f,-0.9709254f,0.2211676f),
            new Vector3(0.4983714f,0.8540927f,-0.1488338f),
            new Vector3(-0.3659533f,0.6485595f,-0.6674196f),
            new Vector3(0.2383734f,-0.4817152f,-0.8432844f),
            new Vector3(0.05949144f,0.8774586f,0.4759488f),
            new Vector3(0.4807356f,-0.3576833f,0.8005973f),
            new Vector3(-0.5566708f,-0.2573237f,-0.7898747f),
            new Vector3(-0.08526214f,0.07761446f,-0.9933309f),
            new Vector3(-0.6676701f,0.150987f,0.7289852f),
            new Vector3(0.3077378f,-0.5990841f,-0.7391858f),
            new Vector3(0.6313454f,-0.2553181f,0.7322674f),
            new Vector3(0.3166381f,-0.559139f,-0.766227f),
            new Vector3(-0.8402355f,-0.1978298f,-0.5048442f),
            new Vector3(-0.2070323f,0.9569677f,-0.2033485f),
            new Vector3(-0.03281602f,-0.02158149f,0.9992284f),
            new Vector3(-0.9410974f,-0.3118985f,0.1305946f),
            new Vector3(-0.5951098f,-0.6401318f,-0.4858761f),
            new Vector3(-0.233086f,-0.9709828f,-0.05350867f),
            new Vector3(-0.6126086f,0.3462378f,0.710514f),
            new Vector3(0.5191658f,0.6349447f,0.5721119f),
            new Vector3(0.4290262f,-0.5082126f,-0.746764f),
            new Vector3(0.3842943f,-0.4937944f,0.7800545f),
            new Vector3(-0.1586605f,-0.8290396f,-0.536209f),
            new Vector3(0.4903165f,0.5396531f,-0.6843715f),
            new Vector3(-0.9447429f,0.2164821f,-0.2461634f),
            new Vector3(-0.8572609f,-0.4866397f,0.168183f),
            new Vector3(0.2016705f,0.758544f,-0.6196289f),
            new Vector3(-0.4402029f,0.4658641f,0.7675886f),
            new Vector3(-0.03031733f,-0.8567787f,0.5147923f),
            new Vector3(-0.4705196f,-0.8808237f,0.05254398f),
            new Vector3(0.8197495f,0.5108191f,-0.2589879f),
            new Vector3(-0.8237348f,-0.2201869f,0.5224736f),
            new Vector3(-0.1594471f,-0.9864077f,-0.0397057f),
            new Vector3(0.8612007f,0.2318807f,0.4522882f),
            new Vector3(-0.4181552f,-0.8495097f,-0.3216824f),
            new Vector3(0.3954279f,0.297698f,-0.8689147f),
            new Vector3(-0.4026274f,0.3530566f,-0.8445367f),
            new Vector3(-0.7271423f,0.2250635f,0.648545f),
            new Vector3(-0.7620295f,0.1674282f,-0.6255229f),
            new Vector3(-0.1091121f,0.8356693f,0.5382857f),
            new Vector3(-0.5048319f,0.8042616f,0.3135414f),
            new Vector3(0.1974159f,-0.9453723f,-0.2594187f),
            new Vector3(0.8161701f,0.2998289f,0.4939322f),
            new Vector3(-0.5658165f,-0.8245294f,0.001719997f),
            new Vector3(-0.2366816f,0.4898423f,-0.8390687f),
            new Vector3(0.6742159f,0.6272649f,0.3898354f),
            new Vector3(0.8043053f,0.5769569f,-0.1421755f),
            new Vector3(-0.8377801f,-0.009850418f,0.545919f),
            new Vector3(0.3399491f,0.4769282f,0.8105394f),
            new Vector3(-0.4669549f,-0.7259183f,0.5049708f),
            new Vector3(0.4577916f,0.3526698f,0.8161194f),
            new Vector3(0.5042316f,0.5911558f,-0.6295121f),
            new Vector3(-0.5619034f,-0.4236732f,-0.7104686f),
            new Vector3(0.4896745f,-0.7189308f,-0.4933126f),
            new Vector3(-0.5171482f,-0.2569599f,-0.8164124f),
            new Vector3(-0.2374545f,-0.9590333f,-0.1545012f),
            new Vector3(-0.05952924f,-0.5791416f,0.8130506f),
            new Vector3(0.6910897f,-0.1526927f,-0.7064559f),
            new Vector3(0.2023576f,-0.001228854f,0.9793109f),
            new Vector3(-0.1406746f,-0.6055886f,0.7832453f),
            new Vector3(0.8599559f,-0.5087409f,-0.04072558f),
            new Vector3(0.9549462f,-0.1425487f,-0.2603028f),
            new Vector3(-0.5774334f,0.6145369f,-0.5375082f),
            new Vector3(0.2156872f,-0.8976552f,0.38431f),
            new Vector3(0.6386496f,-0.3060967f,-0.7059968f),
            new Vector3(0.5149263f,-0.8465654f,-0.1348254f),
            new Vector3(0.40505f,-0.5902152f,-0.6982697f),
            new Vector3(-0.5070248f,0.7744926f,0.3782687f),
            new Vector3(-0.1990385f,-0.6595811f,0.7248009f),
            new Vector3(0.02612923f,-0.3390695f,0.9403985f),
            new Vector3(0.4097165f,-0.0258834f,-0.9118457f),
            new Vector3(0.8946868f,0.439646f,0.07903821f),
            new Vector3(0.9772421f,-0.1791162f,-0.113645f),
            new Vector3(-0.986977f,0.004723408f,-0.1607919f),
            new Vector3(-0.7477787f,0.6356274f,-0.191846f),
            new Vector3(-0.7242022f,-0.03481081f,0.6887084f),
            new Vector3(0.9637696f,0.2283892f,-0.1377919f),
            new Vector3(-0.1778551f,0.6036047f,-0.777193f),
            new Vector3(-0.6320283f,0.06464279f,-0.7722445f),
            new Vector3(-0.4391645f,0.8296401f,-0.3447201f),
            new Vector3(-0.8895322f,0.4166591f,0.1874239f),
            new Vector3(-0.5498042f,0.8262048f,-0.1228865f),
            new Vector3(-0.09829284f,0.7140151f,-0.6931962f),
            new Vector3(-0.8335927f,0.4676093f,0.294049f),
            new Vector3(-0.3566533f,0.9188136f,0.1690559f),
            new Vector3(-0.2138416f,-0.03067158f,0.9763867f),
            new Vector3(-0.03443078f,-0.8606543f,0.5080243f),
            new Vector3(-0.8315732f,-0.38915f,-0.3962933f),
            new Vector3(0.3612817f,-0.1820473f,-0.9145131f),
            new Vector3(0.1838299f,0.8132898f,-0.5520564f),
            new Vector3(0.2917118f,0.3168888f,0.9024886f),
            new Vector3(0.4552357f,-0.7769037f,0.4349495f),
            new Vector3(-0.8114733f,-0.5245482f,-0.257605f),
            new Vector3(-0.5520189f,-0.2990291f,0.778368f),
            new Vector3(0.7805609f,-0.5815777f,0.2291113f),
            new Vector3(0.5727825f,-0.5548559f,0.6033698f),
            new Vector3(-0.1244856f,-0.2258997f,-0.9661639f),
            new Vector3(-0.04149276f,-0.08852574f,-0.9952093f),
            new Vector3(-0.746926f,-0.6645033f,0.02316921f),
            new Vector3(0.2289095f,0.9645985f,0.1309583f),
            new Vector3(-0.2289003f,0.2625867f,-0.9373649f),
            new Vector3(-0.7528787f,0.2995058f,-0.5860632f),
            new Vector3(-0.6321904f,0.496287f,0.595008f),
            new Vector3(0.6299735f,0.3005153f,0.7161173f),
            new Vector3(0.6075312f,0.3535224f,-0.7112861f),
            new Vector3(-0.7017877f,-0.4343964f,-0.5646184f),
            new Vector3(0.7494965f,-0.5372576f,-0.3867937f),
            new Vector3(-0.7985575f,0.5553558f,-0.232133f),
            new Vector3(-0.6859251f,-0.6083732f,-0.3992353f),
            new Vector3(-0.06681218f,-0.9796184f,-0.1894306f),
            new Vector3(0.6672297f,0.7010098f,-0.2517733f),
            new Vector3(0.404022f,-0.2674386f,0.8747816f),
            new Vector3(0.5212709f,-0.5430818f,-0.6582847f),
            new Vector3(-0.7031087f,0.3715872f,0.6062681f),
            new Vector3(-0.3633289f,0.3608747f,0.8589305f),
            new Vector3(0.8402669f,0.526444f,0.1296466f),
            new Vector3(0.9844087f,0.004722823f,0.1758329f),
            new Vector3(-0.1890562f,-0.96938f,-0.1567167f),
            new Vector3(0.2131279f,-0.5385757f,-0.8151764f),
            new Vector3(-0.6529618f,0.3538839f,-0.669632f),
            new Vector3(-0.6632353f,0.1448951f,0.7342508f),
            new Vector3(-0.004977625f,-0.4268819f,-0.9042937f),
            new Vector3(-0.1762059f,-0.3670853f,-0.9133455f),
            new Vector3(0.1676667f,-0.8093696f,-0.5628577f),
            new Vector3(-0.005915863f,0.7091802f,-0.7050025f),
            new Vector3(-0.4531301f,-0.8252673f,0.3370564f),
            new Vector3(-0.6585789f,-0.5457555f,0.5180973f),
            new Vector3(-0.8621657f,-0.5056043f,0.03216504f),
            new Vector3(0.04010008f,-0.8731229f,0.485848f),
            new Vector3(0.4704812f,-0.8786696f,-0.08116173f),
            new Vector3(0.471619f,0.5027894f,-0.7244158f),
            new Vector3(0.1021121f,-0.9853905f,0.136304f),
            new Vector3(0.7086495f,0.5291607f,-0.4666956f),
            new Vector3(0.5540572f,-0.7853456f,0.2761395f),
            new Vector3(0.1890275f,-0.5644084f,0.803562f),
            new Vector3(0.8730552f,-0.3271227f,-0.3616149f),
            new Vector3(0.3976765f,-0.5338131f,-0.7462553f),
            new Vector3(0.6941017f,0.2964479f,-0.6560043f),
            new Vector3(-0.6355512f,0.1190582f,-0.7628236f),
            new Vector3(0.8012667f,-0.01264787f,0.5981737f),
            new Vector3(-0.2339476f,0.9574472f,-0.1690073f),
            new Vector3(0.3010428f,0.9095019f,0.2866697f),
            new Vector3(0.7675132f,0.006025651f,-0.6410049f),
            new Vector3(-0.3232469f,-0.4513432f,-0.8317456f),
            new Vector3(0.09180902f,-0.277474f,-0.9563364f),
            new Vector3(0.3190986f,0.1625821f,0.9336718f),
            new Vector3(0.2015921f,0.1891842f,0.9610255f),
            new Vector3(0.571811f,-0.3061378f,0.7611254f),
            new Vector3(0.5193736f,0.5923042f,0.6159762f),
            new Vector3(-0.5103459f,0.4887131f,-0.7076062f),
            new Vector3(-0.6372817f,0.7106696f,-0.2980281f),
            new Vector3(0.4509937f,-0.3976088f,0.7990693f),
            new Vector3(-0.7815644f,-0.1049065f,-0.6149405f),
            new Vector3(0.4510316f,-0.176696f,0.8748423f),
            new Vector3(-0.433807f,-0.5212471f,0.7349237f),
            new Vector3(-0.4956533f,0.8451114f,0.2002863f),
            new Vector3(0.6199318f,0.1750063f,0.7648904f),
            new Vector3(-0.6811875f,-0.1752569f,0.7108225f),
            new Vector3(-0.4326102f,-0.5100945f,0.7434058f),
            new Vector3(-0.03105433f,-0.2514249f,0.9673784f),
            new Vector3(0.4146106f,0.1251087f,0.9013578f),
            new Vector3(-0.07014231f,0.8875943f,0.4552542f),
            new Vector3(-0.6270158f,-0.1256916f,0.7687997f),
            new Vector3(0.8758535f,0.4557146f,-0.1587605f),
            new Vector3(0.6074404f,0.3356702f,-0.7199595f),
            new Vector3(0.3265508f,-0.07046331f,0.9425495f),
            new Vector3(0.04278179f,0.9982262f,0.04140195f),
            new Vector3(-0.3660043f,0.4591336f,0.8094673f),
            new Vector3(0.1720014f,-0.3199584f,0.9316878f),
            new Vector3(-0.4234209f,0.1663233f,-0.8905343f),
            new Vector3(0.8326372f,0.4780978f,-0.2795314f),
            new Vector3(-0.04025719f,-0.4096435f,0.911357f),
            new Vector3(0.8682814f,0.3467851f,-0.3547218f),
            new Vector3(0.3039192f,-0.2639789f,-0.9153951f),
            new Vector3(0.4326055f,-0.7899793f,-0.4344943f),
            new Vector3(-0.790974f,-0.6065648f,0.08024582f),
            new Vector3(-0.5182332f,-0.07932927f,-0.8515522f),
            new Vector3(-0.7023294f,0.1204915f,-0.7015805f),
            new Vector3(-0.347687f,-0.9365211f,0.04518943f),
            new Vector3(-0.3705507f,-0.6616512f,0.6518512f),
            new Vector3(-0.9624711f,-0.2462981f,-0.1139589f),
            new Vector3(0.5826387f,0.3785676f,-0.7191792f),
            new Vector3(0.9991806f,0.02588714f,-0.03111303f),
            new Vector3(0.1863473f,-0.1544373f,-0.9702699f),
            new Vector3(-0.9766126f,0.213789f,0.02285143f),
            new Vector3(0.002102425f,-0.5131359f,0.8583048f),
            new Vector3(-0.9709717f,0.05841636f,-0.2319516f),
            new Vector3(0.3801872f,0.8792544f,-0.2870005f),
            new Vector3(0.3132635f,0.9183982f,-0.2416829f),
            new Vector3(0.3046843f,0.4103941f,-0.8595024f),
            new Vector3(0.6466078f,0.5999524f,-0.4711216f),
            new Vector3(0.04601396f,0.9873869f,0.151492f),
            new Vector3(0.8903917f,-0.1844497f,-0.4161502f),
            new Vector3(0.7264025f,0.6798574f,0.1006641f),
            new Vector3(0.3672665f,0.9204038f,0.1340606f),
            new Vector3(0.2316772f,0.1805265f,-0.9558953f),
            new Vector3(0.2948472f,-0.7103348f,0.6391318f),
            new Vector3(0.4366646f,-0.8382179f,0.3266723f),
            new Vector3(0.139151f,0.8451409f,0.5161142f),
            new Vector3(-0.6435395f,-0.746819f,-0.1676855f),
            new Vector3(-0.163751f,-0.9234158f,0.3471151f),
            new Vector3(-0.1867879f,0.9762397f,-0.1098474f),
            new Vector3(-0.2359708f,-0.5572494f,0.7961098f),
            new Vector3(-0.01637878f,0.1398417f,0.9900384f),
            new Vector3(-0.7140952f,-0.6330354f,0.2988882f),
            new Vector3(-0.2130667f,-0.933664f,-0.2878785f),
            new Vector3(0.2773764f,0.4948463f,0.8235226f),
            new Vector3(0.5793018f,0.7849221f,0.2197879f),
            new Vector3(0.5264395f,0.240548f,-0.8154742f),
            new Vector3(-0.9852363f,0.1496584f,0.08313637f),
            new Vector3(0.1845386f,0.9734089f,-0.1357221f),
            new Vector3(-0.4247266f,0.9029437f,-0.06557408f),
            new Vector3(0.1576131f,0.744002f,0.649322f),
            new Vector3(-0.7383803f,0.5441383f,-0.3983819f),
            new Vector3(-0.6605289f,-0.7485892f,0.05758378f),
            new Vector3(0.8629102f,0.5008649f,0.06723295f),
            new Vector3(0.6225402f,-0.5135072f,0.5905542f),
            new Vector3(0.5873497f,0.8093328f,-0.0008233867f),
            new Vector3(0.7199305f,0.3444003f,0.6025683f),
            new Vector3(0.4652144f,0.3417616f,0.8165626f),
            new Vector3(0.3449781f,0.06703027f,-0.9362143f),
            new Vector3(-0.2152511f,0.03641683f,0.9758795f),
            new Vector3(-0.8654817f,0.500592f,-0.01868263f),
            new Vector3(0.9996465f,-0.003424279f,0.02636438f),
            new Vector3(0.1226097f,0.4629965f,0.8778389f),
            new Vector3(-0.4830709f,0.3899031f,0.7839758f),
            new Vector3(0.5877575f,0.1025232f,-0.8025149f),
            new Vector3(-0.3655335f,-0.9070789f,0.2087894f),
            new Vector3(-0.8032447f,0.59091f,0.07498982f),
            new Vector3(-0.8876446f,0.4017301f,-0.2251663f),
            new Vector3(0.04926058f,0.9970781f,0.05838408f),
            new Vector3(-0.8559313f,0.1373616f,-0.4985113f),
            new Vector3(0.8321007f,0.007088471f,0.5545791f),
            new Vector3(-0.6807804f,0.723451f,-0.1147026f),
            new Vector3(0.06182511f,0.5068744f,-0.8598f),
            new Vector3(0.0161571f,0.8901687f,-0.4553445f),
            new Vector3(-0.1735543f,0.9173108f,0.358357f),
            new Vector3(0.7616683f,-0.2612129f,-0.5929834f),
            new Vector3(-0.4281045f,-0.532828f,-0.7299458f),
            new Vector3(-0.7496365f,-0.4394424f,0.4949095f),
            new Vector3(-0.6579641f,-0.00429273f,-0.7530372f),
            new Vector3(-0.1163191f,0.9718221f,-0.2050163f),
            new Vector3(-0.1144991f,-0.329388f,-0.9372265f),
            new Vector3(0.505395f,-0.8545362f,-0.1197654f),
            new Vector3(0.4853461f,0.8716518f,-0.06828162f),
            new Vector3(-0.8292785f,-0.02420223f,0.5583112f),
            new Vector3(0.6099254f,0.5160051f,-0.6014397f),
            new Vector3(0.4672442f,0.6058559f,0.6439111f),
            new Vector3(-0.3143614f,-0.687842f,-0.6542556f),
            new Vector3(-0.7222834f,0.1368333f,-0.6779257f),
            new Vector3(-0.1086791f,-0.9937016f,-0.02731312f),
            new Vector3(-0.4325457f,-0.3334087f,0.8377009f),
            new Vector3(-0.1991881f,0.1923451f,0.9608994f),
            new Vector3(-0.9203525f,0.2591792f,-0.2928778f),
            new Vector3(0.7458824f,-0.1558772f,0.6475815f),
            new Vector3(-0.7759466f,-0.5451365f,0.3173846f),
            new Vector3(-0.3315267f,0.361192f,-0.8715678f),
            new Vector3(0.1411204f,0.9543205f,-0.2633578f),
            new Vector3(-0.6789873f,-0.6090345f,-0.409943f),
            new Vector3(-0.5968617f,-0.4052266f,-0.6924936f),
            new Vector3(0.9398619f,0.2664235f,0.2137245f),
            new Vector3(0.8000582f,-0.2585716f,0.5413387f),
            new Vector3(-0.7608832f,0.5842707f,0.2822843f),
            new Vector3(-0.5946699f,-0.5119869f,0.6198685f),
            new Vector3(-0.7674096f,0.4864026f,-0.4177259f),
            new Vector3(0.1031196f,-0.9884334f,-0.1112012f),
            new Vector3(0.1705012f,0.957847f,-0.2312108f),
            new Vector3(-0.9905103f,-0.03617571f,0.1325918f),
            new Vector3(0.2580741f,-0.2084173f,-0.943377f),
            new Vector3(0.234646f,-0.6592218f,0.7144005f),
            new Vector3(0.02323783f,-0.9981394f,0.05637166f),
            new Vector3(-0.5512204f,-0.6080416f,0.5713505f),
            new Vector3(0.9677451f,0.2336775f,0.0941501f),
            new Vector3(-0.4211915f,-0.2272382f,-0.8780436f),
            new Vector3(-0.09671877f,-0.995038f,-0.02334267f),
            new Vector3(0.07079554f,-0.9619496f,-0.2638956f),
            new Vector3(-2.283617E-05f,-0.6046172f,0.7965161f),
            new Vector3(0.7837385f,-0.6204609f,0.02796916f),
            new Vector3(-0.2735063f,-0.3030322f,-0.9128887f),
            new Vector3(0.5191659f,0.8526559f,-0.05869097f),
            new Vector3(-0.3177048f,-0.7025355f,0.6367948f),
            new Vector3(-0.3557645f,0.09549253f,0.9296842f),
            new Vector3(0.8377282f,-0.4309491f,0.3354018f),
            new Vector3(-0.9999204f,0.009680816f,-0.008090522f),
            new Vector3(-0.04106909f,-0.310443f,0.9497044f),
            new Vector3(0.4052077f,0.6757398f,-0.6157779f),
            new Vector3(-0.9427654f,-0.2859582f,0.1715268f),
            new Vector3(0.232333f,-0.9634795f,0.1331484f),
            new Vector3(-0.5846712f,-0.6076342f,-0.5375317f),
            new Vector3(-0.8317611f,-0.2902159f,-0.4732317f),
            new Vector3(0.3429927f,-0.8664067f,-0.3628986f),
            new Vector3(0.05830181f,0.8979526f,-0.4362133f),
            new Vector3(0.8788754f,-0.4002544f,0.2595658f),
            new Vector3(-0.949056f,-0.2619729f,-0.1751084f),
            new Vector3(-0.6966791f,0.3247408f,0.6396732f),
            new Vector3(0.06620698f,-0.8167391f,0.5731962f),
            new Vector3(-0.3773842f,-0.06710587f,-0.9236222f),
            new Vector3(0.6615676f,0.7430451f,0.1010553f),
            new Vector3(0.745782f,0.6234754f,-0.2347079f),
            new Vector3(-0.9550944f,-0.06077048f,0.2900027f),
            new Vector3(-0.1958052f,0.9391175f,0.2823448f),
            new Vector3(0.01949587f,-0.9994414f,-0.02714264f),
            new Vector3(-0.4774897f,-0.5885444f,0.652395f),
            new Vector3(0.1963242f,-0.2838114f,-0.9385669f),
            new Vector3(0.5839729f,-0.236478f,-0.7765654f),
            new Vector3(0.894231f,0.4409218f,0.07706382f),
            new Vector3(0.1057622f,-0.4611742f,0.8809839f),
            new Vector3(-0.9105714f,-0.2875688f,0.2969241f),
            new Vector3(-0.7097519f,0.3180765f,-0.6285536f),
            new Vector3(0.8928577f,0.4335882f,0.1216816f),
            new Vector3(0.2375094f,-0.6701362f,-0.7032117f),
            new Vector3(-0.9873676f,-0.07425085f,0.1399718f),
            new Vector3(0.5804785f,0.3396118f,-0.7400733f),
            new Vector3(0.6350856f,0.7044261f,0.3169385f),
            new Vector3(0.8813507f,-0.02113431f,0.4719897f),
            new Vector3(0.943159f,-0.3200823f,-0.08943379f),
            new Vector3(-0.6043115f,-0.5823601f,0.5437502f),
            new Vector3(0.1463377f,0.9028676f,-0.4042468f),
            new Vector3(-0.6082162f,0.5740712f,0.5481927f),
            new Vector3(0.01421209f,-0.2809225f,0.9596252f),
            new Vector3(-0.4264157f,0.8872196f,0.1760994f),
            new Vector3(0.8430799f,0.5373437f,-0.02186135f),
            new Vector3(-0.3313412f,-0.934745f,0.1283153f),
            new Vector3(0.3061066f,0.5577919f,-0.7714706f),
            new Vector3(0.02809231f,-0.1242646f,0.9918513f),
            new Vector3(0.06408694f,0.9371349f,0.3430319f),
            new Vector3(-0.6597242f,-0.4363124f,-0.6118787f),
            new Vector3(0.6699499f,0.6399721f,0.3763016f),
            new Vector3(0.6321271f,-0.6223249f,0.4616566f),
            new Vector3(0.6191813f,-0.223206f,-0.752857f),
            new Vector3(0.838947f,-0.2766634f,0.468642f),
            new Vector3(-0.4804573f,-0.8597824f,-0.1730175f),
            new Vector3(-0.9073731f,0.3323419f,0.2573381f),
            new Vector3(-0.1290742f,-0.2560616f,-0.9580044f),
            new Vector3(-0.6144979f,-0.6644081f,0.4253871f),
            new Vector3(0.7770497f,0.497189f,0.3860011f),
            new Vector3(-0.6702635f,-0.6476172f,-0.3624069f),
            new Vector3(0.2324043f,-0.2988929f,-0.9255545f),
            new Vector3(-0.5025815f,-0.5870031f,-0.6346961f),
            new Vector3(0.5504668f,-0.8214068f,-0.1492553f),
            new Vector3(0.3295548f,0.5118419f,-0.7933546f),
            new Vector3(-0.1957809f,0.374616f,0.906274f),
            new Vector3(0.9269456f,0.3648379f,0.08755044f),
            new Vector3(-0.7863624f,0.5951239f,0.1657156f),
            new Vector3(-0.4008425f,-0.2223398f,-0.8887578f),
            new Vector3(0.4095579f,0.6289102f,-0.6608588f),
            new Vector3(-0.1078771f,-0.1129878f,-0.9877228f),
            new Vector3(-0.8373255f,-0.5222918f,0.1615466f),
            new Vector3(-0.4078346f,-0.377867f,0.8311964f),
            new Vector3(-0.7509521f,0.2134206f,0.6249181f),
            new Vector3(-0.5534031f,-0.1001308f,-0.8268729f),
            new Vector3(0.8982056f,0.1581247f,0.4101502f),
            new Vector3(-0.4249438f,0.1774961f,0.8876474f),
            new Vector3(-0.04396572f,0.4691513f,0.8820227f),
            new Vector3(0.5062927f,0.5642397f,-0.6521513f),
            new Vector3(-0.7228364f,-0.5185949f,0.4566911f),
            new Vector3(-0.2234437f,0.7982721f,-0.5593162f),
            new Vector3(-0.7531384f,0.614691f,-0.2343877f),
            new Vector3(0.837971f,0.364368f,0.4062519f),
            new Vector3(0.3665504f,-0.3786686f,0.8498534f),
            new Vector3(-0.6225281f,-0.2266951f,0.7490448f),
            new Vector3(0.6885368f,0.5760597f,0.4405363f),
            new Vector3(-0.7076105f,-0.2957029f,0.6417532f),
            new Vector3(0.5377229f,0.1838757f,0.8228267f),
            new Vector3(0.7971628f,-0.6020327f,-0.04569557f),
            new Vector3(0.6270494f,0.002523662f,-0.7789755f),
            new Vector3(-0.5471564f,0.3396142f,-0.7650373f),
            new Vector3(-0.6406748f,0.6935254f,-0.3294819f),
            new Vector3(-0.9269385f,-0.002108139f,0.3752074f),
            new Vector3(0.03206816f,-0.9835041f,0.1780208f),
            new Vector3(-0.1415447f,0.6855717f,-0.7141124f),
            new Vector3(-0.3775954f,-0.4120342f,0.8292464f),
            new Vector3(0.5402759f,-0.8276125f,0.1521828f),
            new Vector3(0.4622907f,-0.7462043f,-0.4790266f),
            new Vector3(0.7655651f,-0.1337186f,-0.6293087f),
            new Vector3(-0.3111013f,-0.5538006f,0.7723477f),
            new Vector3(-0.2917601f,0.01231852f,0.9564123f),
            new Vector3(0.2634835f,-0.1096961f,-0.9584066f),
            new Vector3(-0.6935104f,0.6500599f,-0.3105891f),
            new Vector3(0.08166447f,0.9375939f,0.3380069f),
            new Vector3(-0.05863496f,-0.5835003f,0.8099934f),
            new Vector3(0.279132f,0.9190654f,-0.2782157f),
            new Vector3(-0.07360857f,-0.1735463f,0.982071f),
            new Vector3(-0.07896122f,0.9728496f,0.2175518f),
            new Vector3(-0.5085377f,-0.7561564f,-0.4118458f),
            new Vector3(0.6626825f,-0.7366308f,-0.135007f),
            new Vector3(-0.8283473f,0.5598538f,-0.02011362f),
            new Vector3(0.7516595f,-0.002613049f,0.6595461f),
            new Vector3(-0.04448146f,0.4735667f,0.879634f),
            new Vector3(-0.7853576f,0.5772697f,-0.2235465f),
            new Vector3(0.5251135f,-0.5524506f,0.6473438f),
            new Vector3(-0.3676612f,-0.4082185f,-0.8355734f),
            new Vector3(0.5500224f,0.6692851f,0.4995326f),
            new Vector3(-0.5534413f,-0.6190237f,-0.5572364f),
            new Vector3(0.813864f,0.2318247f,-0.5328065f),
            new Vector3(-0.2229709f,-0.5782655f,0.7847885f),
            new Vector3(-0.8667825f,-0.4277771f,0.2563102f),
            new Vector3(-0.8094573f,0.5310439f,0.250542f),
            new Vector3(0.4056306f,0.8156186f,-0.4125895f),
            new Vector3(-0.0589058f,0.1805307f,0.9818038f),
            new Vector3(0.5999011f,0.7102728f,0.3682814f),
            new Vector3(0.3672219f,0.2934379f,-0.8826337f),
            new Vector3(-0.3270554f,-0.7660099f,-0.5534108f),
            new Vector3(-0.5886025f,-0.6581171f,-0.4694987f),
            new Vector3(0.06916948f,0.02042043f,0.9973959f),
            new Vector3(-0.8466849f,0.3099375f,-0.4325083f),
            new Vector3(-0.5575005f,0.5898023f,0.5842315f),
            new Vector3(0.1072613f,-0.3590479f,-0.9271352f),
            new Vector3(0.2556283f,-0.9663495f,0.02868402f),
            new Vector3(0.1752471f,0.5536702f,-0.814087f),
            new Vector3(0.8391794f,0.002962032f,-0.5438466f),
            new Vector3(0.09885073f,-0.4331636f,-0.8958782f),
            new Vector3(-0.8118838f,0.557242f,0.1741438f),
            new Vector3(0.5149977f,0.102506f,0.8510405f),
            new Vector3(0.9631078f,-0.2261948f,0.1458056f),
            new Vector3(0.4448927f,0.7939807f,-0.4143249f),
            new Vector3(-0.47204f,-0.7196285f,0.5092279f),
            new Vector3(-0.3190016f,-0.5898644f,0.7418206f),
            new Vector3(0.6271433f,0.06410856f,-0.7762612f),
            new Vector3(0.8356157f,-0.02828878f,0.5485856f),
            new Vector3(-0.768006f,-0.5600795f,0.3106088f),
            new Vector3(-0.4015334f,-0.9134374f,0.06635432f),
            new Vector3(-0.2043467f,0.4003075f,0.8933064f),
            new Vector3(-0.9547167f,0.05192117f,0.292951f),
            new Vector3(-0.2383036f,0.9489032f,-0.2068673f),
            new Vector3(-0.3702665f,-0.5461307f,0.7514279f),
            new Vector3(0.7169797f,0.5299486f,-0.4528737f),
            new Vector3(0.434483f,0.4225123f,-0.7954294f),
            new Vector3(-0.8598974f,-0.5103588f,0.01051138f),
            new Vector3(-0.2939389f,-0.1703678f,-0.9405184f),
            new Vector3(0.01866743f,-0.4262976f,-0.9043903f),
            new Vector3(0.3455132f,0.9283352f,-0.1371657f),
            new Vector3(-0.2073569f,-0.385879f,0.8989441f),
            new Vector3(0.5701829f,-0.6460713f,0.5074283f),
            new Vector3(0.9232683f,0.2494971f,-0.2921077f),
            new Vector3(-0.508081f,0.7408628f,0.4392904f),
            new Vector3(-0.6709898f,0.09780377f,0.7349878f),
            new Vector3(-0.6950539f,0.003607549f,-0.7189485f),
            new Vector3(0.1525845f,-0.9106672f,0.3839312f),
            new Vector3(-0.2623563f,-0.8571244f,-0.443291f),
            new Vector3(-0.288149f,0.1233448f,0.9496084f),
            new Vector3(0.01880201f,0.9495444f,0.3130687f),
            new Vector3(-0.9281792f,0.3648982f,0.07302496f),
            new Vector3(-0.514792f,-0.2518123f,0.8194996f),
            new Vector3(-0.5471275f,0.5049381f,-0.6675995f),
            new Vector3(-0.6076618f,0.2437051f,-0.7558802f),
            new Vector3(-0.2626085f,-0.9009501f,0.3454354f),
            new Vector3(0.8027387f,0.3134469f,0.5073082f),
            new Vector3(-0.2196754f,-0.03151661f,0.9750639f),
            new Vector3(-0.1257433f,-0.5085447f,-0.8518044f),
            new Vector3(-0.6117875f,0.7694378f,-0.1835251f),
            new Vector3(0.435317f,-0.7171371f,0.544255f),
            new Vector3(-0.9236638f,0.3320819f,-0.1912244f),
            new Vector3(-0.9983269f,0.001130348f,0.05781087f),
            new Vector3(-0.8861473f,0.3966126f,0.2396694f),
            new Vector3(0.8644748f,-0.4614426f,0.1993844f),
            new Vector3(0.2390508f,0.9694477f,0.05500742f),
            new Vector3(0.5253181f,0.5251979f,-0.6694835f),
            new Vector3(0.2918615f,-0.2015962f,-0.9349737f),
            new Vector3(-0.3212497f,0.4393734f,-0.8388979f),
            new Vector3(-0.6625966f,0.6549898f,0.363255f),
            new Vector3(-0.264226f,-0.2719215f,-0.9253341f),
            new Vector3(-0.6997954f,0.6847068f,0.2036246f),
            new Vector3(0.9572368f,0.2889183f,-0.01496185f),
            new Vector3(0.1095404f,-0.9701689f,0.2162709f),
            new Vector3(-0.7747203f,-0.5086094f,-0.3756661f),
            new Vector3(-0.348971f,-0.4705632f,0.8104255f),
            new Vector3(-0.2442682f,-0.5707004f,-0.783986f),
            new Vector3(-0.5273347f,0.5942875f,-0.60724f),
            new Vector3(-0.9687754f,-0.1120207f,0.2211912f),
            new Vector3(-0.2167856f,-0.5689813f,-0.7932618f),
            new Vector3(-0.9173925f,0.3927826f,-0.06413131f),
            new Vector3(-0.7228039f,-0.6575696f,0.2125008f),
            new Vector3(0.3753873f,0.92394f,0.07361562f),
            new Vector3(0.3398977f,0.2811118f,-0.8974663f),
            new Vector3(0.9927892f,-0.09747819f,-0.0697679f),
            new Vector3(-0.1728838f,0.06519743f,-0.982782f),
            new Vector3(-0.08567473f,0.855784f,0.5101898f),
            new Vector3(-0.102903f,0.7052814f,-0.7014194f),
            new Vector3(-0.9774728f,-0.0569295f,-0.2032385f),
            new Vector3(0.4882668f,-0.7978297f,0.3536429f),
            new Vector3(-0.5966586f,-0.4532213f,0.6622604f),
            new Vector3(-0.3550596f,-0.9317738f,-0.07569902f),
            new Vector3(0.4310912f,0.5821934f,0.6893557f),
            new Vector3(-0.6014804f,0.0640096f,0.7963191f),
            new Vector3(-0.0183252f,-0.930521f,-0.3657799f),
            new Vector3(0.1174036f,-0.1698189f,-0.9784569f),
            new Vector3(-0.7272784f,-0.6620013f,0.181164f),
            new Vector3(0.2705164f,-0.4326281f,0.8600314f),
            new Vector3(0.5343285f,-0.4168993f,-0.735315f),
            new Vector3(-0.06562884f,-0.9971116f,0.03822797f),
            new Vector3(-0.2431875f,-0.7286189f,-0.6402923f),
            new Vector3(0.8377117f,0.5459488f,0.01338044f),
            new Vector3(-0.3024424f,-0.563265f,0.7689351f),
            new Vector3(0.6854804f,0.7222363f,-0.09214891f),
            new Vector3(0.7185082f,-0.1995294f,0.6662837f),
            new Vector3(0.4511791f,0.7618496f,0.4647824f),
            new Vector3(0.4049247f,-0.9135455f,-0.03834647f),
            new Vector3(-0.820199f,-0.08809751f,0.5652542f),
            new Vector3(-0.1703553f,-0.7777299f,-0.6050746f),
            new Vector3(0.7440475f,0.02239823f,-0.6677511f),
            new Vector3(-0.7875978f,0.3818246f,-0.4836318f),
            new Vector3(-0.5505305f,-0.7204742f,-0.4217025f),
            new Vector3(-0.3840175f,0.1651037f,-0.9084445f),
            new Vector3(-0.9873534f,-0.1237189f,0.09913068f),
            new Vector3(0.9950786f,-0.09778016f,-0.0160514f),
            new Vector3(0.04341194f,-0.9549794f,0.2934788f),
            new Vector3(-0.8104571f,0.264893f,-0.5224854f),
            new Vector3(-0.1648202f,-0.9830221f,0.08063333f),
            new Vector3(-0.7764662f,0.6178532f,0.1239265f),
            new Vector3(-0.295133f,-0.3132314f,0.9026531f),
            new Vector3(-0.1820703f,-0.9195099f,-0.348356f),
            new Vector3(0.20912f,-0.1503292f,-0.966266f),
            new Vector3(0.4536615f,-0.8464163f,-0.2788739f),
            new Vector3(0.7794167f,-0.6070879f,-0.1547704f),
            new Vector3(0.7618909f,0.6123872f,-0.21096f),
            new Vector3(0.4682619f,-0.7966503f,-0.3822029f),
            new Vector3(-0.4923117f,-0.3858857f,-0.780206f),
            new Vector3(-0.2459372f,-0.8443589f,0.4759967f),
            new Vector3(0.6273772f,0.7652224f,-0.1443346f),
            new Vector3(-0.109284f,-0.2002107f,-0.973639f),
            new Vector3(0.9934502f,0.07883718f,0.08271248f),
            new Vector3(-0.9004489f,-0.3481598f,-0.2607233f),
            new Vector3(-0.7500507f,0.3428011f,-0.5656071f),
            new Vector3(0.27399f,0.8991441f,0.3412761f),
            new Vector3(0.850066f,0.3552909f,0.3887881f),
            new Vector3(0.3453864f,-0.322873f,0.8811704f),
            new Vector3(-0.8270005f,-0.4346233f,-0.3566129f),
            new Vector3(-0.8079973f,-0.2810561f,0.5178298f),
            new Vector3(0.5587747f,0.8070421f,-0.1909293f),
            new Vector3(-0.7735817f,0.6336844f,0.003931501f),
            new Vector3(0.6320419f,0.4148492f,-0.6545404f),
            new Vector3(-0.07797448f,-0.6983735f,0.7114735f),
            new Vector3(0.0581479f,-0.9572861f,0.2832353f),
            new Vector3(0.0002021412f,0.1720149f,0.9850942f),
            new Vector3(-0.2329823f,0.570356f,-0.7876632f),
            new Vector3(-0.5005043f,-0.3315108f,0.7997475f),
            new Vector3(0.2006449f,-0.3282147f,-0.9230476f),
            new Vector3(0.9203871f,-0.2372476f,-0.3108072f),
            new Vector3(-0.02578077f,-0.6395681f,0.768302f),
            new Vector3(0.7909284f,-0.6002072f,0.1190948f),
            new Vector3(0.3776088f,-0.9011574f,0.2129013f),
            new Vector3(0.6613932f,0.7070987f,-0.250141f),
            new Vector3(-0.9271903f,0.04486366f,0.3718944f),
            new Vector3(-0.5325696f,-0.7023929f,-0.4722435f),
            new Vector3(0.2991729f,0.2762498f,-0.9133354f),
            new Vector3(0.02826209f,0.08209439f,0.9962237f),
            new Vector3(0.09033354f,0.2266455f,0.9697791f),
            new Vector3(-0.5110532f,-0.1643816f,-0.8436844f),
            new Vector3(-0.8140771f,-0.5797735f,-0.03377853f),
            new Vector3(-0.5935367f,-0.210546f,-0.7767783f),
            new Vector3(0.6790305f,-0.6285313f,-0.3792967f),
            new Vector3(-0.2116914f,0.419726f,0.8826193f),
            new Vector3(-0.1235953f,0.494004f,0.8606301f),
            new Vector3(0.06129435f,-0.9351751f,0.3488418f),
            new Vector3(0.5852997f,0.05975461f,-0.8086122f),
            new Vector3(-0.9022018f,0.3174476f,0.2919915f),
            new Vector3(0.4681811f,0.8721022f,0.1422822f),
            new Vector3(0.02745539f,0.4943427f,-0.8688334f),
            new Vector3(-0.03553491f,-0.9405169f,0.3378836f),
            new Vector3(0.9730472f,-0.164983f,-0.1611203f),
            new Vector3(-0.7967182f,0.109537f,0.5943415f),
            new Vector3(0.234649f,0.9255639f,0.2971049f),
            new Vector3(0.7656271f,0.6337202f,0.1105163f),
            new Vector3(0.773185f,-0.5448423f,-0.3245487f),
            new Vector3(-0.4808864f,0.8432084f,-0.240308f),
            new Vector3(-0.5553807f,0.7222689f,0.4121649f),
            new Vector3(-0.890013f,0.116219f,-0.4408741f),
            new Vector3(-0.6175111f,0.04977528f,0.7849856f),
            new Vector3(-0.5212922f,-0.849065f,-0.08569165f),
            new Vector3(-0.5057516f,-0.7461735f,-0.4329439f),
            new Vector3(0.1546583f,-0.9877103f,-0.02256417f),
            new Vector3(-0.4593479f,0.04645517f,-0.8870409f),
            new Vector3(-0.1682557f,0.9765928f,0.134002f),
            new Vector3(0.8073789f,0.5221978f,-0.2746794f),
            new Vector3(-0.5882161f,0.5613443f,0.5821463f),
            new Vector3(-0.2696396f,-0.02561098f,-0.9626207f),
            new Vector3(-0.1624886f,-0.5770543f,-0.8003786f),
            new Vector3(0.6768038f,-0.7343386f,-0.05180157f),
            new Vector3(0.7475651f,0.220917f,-0.6263722f),
            new Vector3(-0.5125013f,0.8510953f,-0.1139264f),
            new Vector3(0.07038245f,-0.6285918f,0.7745442f),
            new Vector3(-0.7446268f,0.2015712f,0.6363175f),
            new Vector3(0.7049422f,-0.6735075f,-0.2223602f),
            new Vector3(-0.876559f,-0.1005965f,-0.4706643f),
            new Vector3(0.1792308f,-0.9080501f,-0.378578f),
            new Vector3(-0.338983f,-0.228722f,0.9125661f),
            new Vector3(-0.4552119f,-0.06400957f,-0.8880793f),
            new Vector3(0.1590362f,0.9868047f,0.03039545f),
            new Vector3(0.3923255f,-0.9197868f,0.008542115f),
            new Vector3(-0.08395737f,-0.9320533f,-0.3524595f),
            new Vector3(0.9196392f,0.3917122f,0.02872899f),
            new Vector3(0.5673214f,0.395035f,-0.7225606f),
            new Vector3(-0.2598195f,0.9605151f,-0.09952153f),
            new Vector3(-0.4488603f,0.7441011f,0.4948111f),
            new Vector3(-0.4993183f,0.6450491f,0.5784401f),
            new Vector3(0.7680377f,0.2812678f,-0.5753316f),
            new Vector3(0.7063535f,-0.4576486f,-0.5400208f),
            new Vector3(0.3923034f,0.8710312f,-0.2956394f),
            new Vector3(0.8261536f,0.5596491f,0.0652924f),
            new Vector3(-0.9280189f,-0.3684479f,0.05501885f),
            new Vector3(0.9849212f,0.1153001f,-0.1289808f),
            new Vector3(-0.4132621f,0.07439795f,0.9075679f),
            new Vector3(-0.6153998f,0.7857587f,-0.06217983f),
            new Vector3(0.1050852f,0.8758576f,0.4709891f),
            new Vector3(-0.7378512f,0.04511893f,0.6734537f),
            new Vector3(-0.1664592f,0.1613527f,-0.9727573f),
            new Vector3(0.1761739f,-0.5281179f,-0.830695f),
            new Vector3(-0.7916301f,0.1210987f,-0.5988796f),
            new Vector3(0.2315696f,-0.225489f,0.9463246f),
            new Vector3(0.6971126f,-0.5552528f,0.4535729f),
            new Vector3(-0.06635895f,0.8730437f,-0.4831058f),
            new Vector3(0.08992885f,-0.7824047f,-0.6162432f),
            new Vector3(-0.1341861f,0.9580179f,-0.2533687f),
            new Vector3(-0.3626071f,0.4196107f,-0.8321316f),
            new Vector3(-0.9725513f,-0.02607386f,0.2312228f),
            new Vector3(0.7980587f,-0.5929806f,-0.1071277f),
            new Vector3(0.8626877f,-0.4796426f,-0.1603524f),
            new Vector3(0.7977957f,-0.08194663f,-0.5973332f),
            new Vector3(0.677837f,-0.004105396f,0.7352008f),
            new Vector3(0.9059561f,-0.003742639f,0.4233549f),
            new Vector3(-0.5926865f,0.798585f,-0.1048076f),
            new Vector3(0.1616268f,-0.3464349f,0.9240453f),
            new Vector3(-0.4491371f,-0.8599354f,0.2424601f),
            new Vector3(0.9218143f,-0.1476717f,-0.3584014f),
            new Vector3(0.824334f,-0.5653267f,-0.02965352f),
            new Vector3(0.1043314f,-0.7213546f,0.6846623f),
            new Vector3(-0.483103f,-0.4977071f,-0.7203466f),
            new Vector3(-0.1940882f,-0.3928016f,-0.8989086f),
            new Vector3(-0.852083f,-0.3269201f,-0.4087515f),
            new Vector3(0.9265702f,-0.3100097f,0.2129824f),
            new Vector3(-0.0662099f,-0.6748857f,0.734946f),
            new Vector3(-0.5404311f,-0.6911482f,-0.479842f),
            new Vector3(-0.3288642f,-0.04343821f,0.9433777f),
            new Vector3(-0.698575f,-0.6345751f,-0.3306168f),
            new Vector3(0.8788996f,0.4735999f,0.05690808f),
            new Vector3(-0.2533782f,0.8338053f,0.4904775f),
            new Vector3(-0.3729351f,-0.1069875f,0.9216686f),
            new Vector3(-0.9424383f,-0.3198623f,0.09745871f),
            new Vector3(-0.6931694f,-0.4165219f,-0.5882395f),
            new Vector3(-0.8991746f,0.3666509f,0.2388557f),
            new Vector3(-0.5177453f,-0.8032966f,-0.2943713f),
            new Vector3(0.8757619f,0.4545643f,0.1625187f),
            new Vector3(-0.8449939f,-0.1906303f,0.4996452f),
            new Vector3(-0.9773207f,-0.2035588f,0.05838016f),
            new Vector3(0.4687381f,0.8790893f,0.08652467f),
            new Vector3(0.6190376f,-0.43761f,0.6521426f),
            new Vector3(-0.3693388f,-0.906355f,-0.2052061f),
            new Vector3(-0.7328889f,-0.2143305f,0.645706f),
            new Vector3(0.7481341f,0.6576066f,0.08859475f),
            new Vector3(-0.2250786f,0.7205279f,-0.6558805f),
            new Vector3(-0.8658776f,0.4457323f,0.2271098f),
            new Vector3(0.8159556f,-0.3623739f,-0.450446f),
            new Vector3(0.5539111f,-0.4461697f,-0.7029333f),
            new Vector3(-0.1906687f,0.8459145f,0.4980705f),
            new Vector3(0.2230763f,0.7712856f,-0.596117f),
            new Vector3(0.3352223f,0.4566029f,0.8240994f),
            new Vector3(0.8854387f,-0.3838378f,0.2620432f),
            new Vector3(-0.3895826f,-0.8676792f,0.3088012f),
            new Vector3(0.0749817f,-0.9893377f,0.1248544f),
            new Vector3(0.7749546f,-0.3063799f,0.5527899f),
            new Vector3(-0.3175655f,0.6748266f,0.666154f),
            new Vector3(0.8864341f,0.359411f,0.2916478f),
            new Vector3(-0.1427825f,-0.8917135f,-0.4294885f),
            new Vector3(0.2313512f,0.7007843f,0.674817f),
            new Vector3(-0.5525596f,-0.1492807f,-0.8199959f),
            new Vector3(0.4239262f,-0.8615946f,-0.2791797f),
            new Vector3(0.7043914f,-0.6842986f,-0.1885951f),
            new Vector3(-0.6794441f,-0.6568083f,0.3270452f),
            new Vector3(0.8069187f,0.2793812f,-0.5204117f),
            new Vector3(0.003798748f,-0.251047f,0.9679675f)
        };
    }

