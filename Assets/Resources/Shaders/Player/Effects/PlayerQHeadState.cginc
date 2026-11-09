#ifndef ESPOIR_HEAD_STATE
#define ESPOIR_HEAD_STATE

#define FX_MESH_SCALE 6.0

float _HScale, _HLen, _HLengthen, _HCharge, _HReveal, _HBack, _HLunge, _HHold, _HExit, _HOvershoot, _HPop, _HDash,
    _HHotHold;

struct HeadState
{
    float reveal, off, sc, hot, dis, alpha, wob;
};

float HeadEaseOut(float t)
{
    float k = 1.0 - t;
    return 1.0 - k * k * k;
}

HeadState HeadAt(float age)
{
    float T1 = _HCharge, T2 = T1 + _HLunge, T3 = T2 + _HHold;
    HeadState s;
    s.reveal = 1.05;
    s.off = 0.0;
    s.sc = 1.0;
    s.hot = 0.3;
    s.dis = 0.0;
    s.alpha = 1.0;
    if (age < 0.0)
    {
        s.reveal = 0.0;
        s.off = -_HBack;
        s.alpha = 0.0;
    }

    else if (age < T1)
    {
        float c = age / T1, e = c * c;
        s.reveal = _HReveal * HeadEaseOut(c);
        s.off = -_HBack;
        s.sc = 0.85 + 0.05 * e;
        s.hot = 0.25 + 0.25 * c;
        s.alpha = saturate(age / 0.03);
    }

    else if (age < T2)
    {
        float e = HeadEaseOut((age - T1) / _HLunge);
        s.reveal = _HReveal + (1.05 - _HReveal) * e;
        s.off = -_HBack + (_HBack + _HOvershoot) * e;
        s.sc = 0.9 + (0.1 + _HPop) * e;
        s.hot = 1.0;
    }

    else if (age < T3)
    {
        float c = (age - T2) / _HHold, e = HeadEaseOut(c);
        s.off = _HOvershoot * (1.0 - e) + 0.08 * c;
        s.sc = 1.0 + _HPop * (1.0 - e);
        s.hot = _HHotHold + (1.0 - _HHotHold) * (1.0 - HeadEaseOut(saturate((age - T2) / 0.06)));
    }

    else
    {
        float c = saturate((age - T3) / _HExit), e = 1.0 - (1.0 - c) * (1.0 - c);
        s.off = 0.08 - _HDash * e;
        s.sc = 1.0 - 0.1 * e;
        s.hot = _HHotHold * (1.0 - c);
        s.dis = c;
    }
    s.wob = age < T2 ? 0.0 : 1.0 - saturate((age - T2) / (_HHold + _HExit));
    return s;
}

float3x3 HeadWobble(float t, float wob)
{
    float a = sin(t * 23.0) * 0.07 * wob, b = sin(t * 15.0 + 1.0) * 0.05 * wob, c = sin(t * 19.0 + 2.0) * 0.04 * wob;
    float ca = cos(a), sa = sin(a), cb = cos(b), sb = sin(b), cc = cos(c), sc = sin(c);
    return float3x3(
        cb * cc, -cb * sc, sb,
        ca * sc + sa * sb * cc, ca * cc - sa * sb * sc, -sa * cb,
        sa * sc - ca * sb * cc, sa * cc + ca * sb * sc, ca * cb);
}

float3 HeadScaleOf(HeadState s)
{
    return float3(1.0, 1.0, _HLengthen) * _HScale * s.sc;
}
float3 HeadPlace(float3 q, HeadState s, float t)
{
    return mul(HeadWobble(t, s.wob), q * HeadScaleOf(s)) + float3(0.0, 0.0, s.off * _HLen * _HScale);
}

struct FxBasis
{
    float3 right, up, fwd;
};
FxBasis FxBasisOf(float3 tangent)
{
    FxBasis b;
    b.fwd = normalize(tangent);
    b.up = float3(0.0, 1.0, 0.0);
    b.right = normalize(cross(b.up, b.fwd));
    return b;
}
float3 FxToModel(FxBasis b, float3 d)
{
    return float3(-dot(d, b.right), dot(d, b.up), dot(d, b.fwd));
}
float3 FxFromModel(FxBasis b, float3 p)
{
    return b.right * -p.x + b.up * p.y + b.fwd * p.z;
}
#endif
