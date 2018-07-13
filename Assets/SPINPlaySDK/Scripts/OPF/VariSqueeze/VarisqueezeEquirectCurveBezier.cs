using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Bob's Bezier based VariSqueezecode ported from C++ to C#
// Bob's comments intact. Some of C++ code left as reference

public class VarisqueezeEquirectCurveBezier   {

    public float m_Xa;
    public float m_Xb;
    public float m_Xc;
    public Vector2 m_P0;
    public Vector2 m_P1;
    public Vector2 m_P2;
    public Vector2 m_P3;
    public Vector2 m_P4;
    public Vector2 m_P5;

	public Bezier3 m_B0;
    public Bezier3 m_B1;

	public VarisqueezeEquirectCurveBezier()
	{

	}

	


    bool compareRange(float a, float b)
    {
        bool result = Mathf.Abs(a - 0.5f) < Mathf.Abs(b - 0.5f);
        return result;
    }


    // Get the t value from the given array that is closest to 0.5 (i.e. nearest
    // the centre of the 0 - 1 range). This gives us a way of getting the best
    // t value even if rounding errors have put it just outsude the 0 - 1 range.
    float getTClosestToRange(List<float>     v)
    {
		 
		float latest = 100000.0f;
		 
// DoubleVector::const_iterator i(adobe::min_element(v, compareRange));
    //    return *i;

	foreach(float theFloat in v)
     {
        bool  floatLessThan=  compareRange(theFloat,latest);
		if(floatLessThan)
		{
			latest =theFloat;
		}


     }
//	 Debug.Log("Latest: " + latest);
		return latest;
    }

    float getSingleY(Bezier3    b, float x)
    {
        float  t = (getTClosestToRange(b.getTFromX(x)));

        Vector2  xy = (b.getCoordinates(t));

        return xy.y;
    }

    float getSingleX(Bezier3   b, float y)
    {
        float   t = (getTClosestToRange(b.getTFromY(y)));

        Vector2   xy = (b.getCoordinates(t));

        return xy.x;
    }

public void SetupBezier(
    int equirectWidth, int varisqueezeWidth, int identityWidth, double smoothness)
    {


        // This is a pain to explain without pictures and hand waving.
        // m_Xa, m_Xb and m_Xc are distances from the left side of the equirect
        // image. They help define the various points we need for the curve.
        float k = 1.0f - (float)smoothness;
        m_Xa = (equirectWidth - varisqueezeWidth) / 2;
        m_Xb = (varisqueezeWidth - identityWidth) / 2;
        m_Xc = identityWidth;

        m_P0.x = 0.0f;
        m_P0.y = 0.0f;

float   m_P1x = (m_Xb * k);
        m_P1.x = m_P1x;
        m_P1.y = m_Xa + m_P1x;
        m_P2.x = m_Xb;
        m_P2.y = m_Xa + m_Xb;

        m_P3.x = m_Xb + m_Xc;
        m_P3.y = m_Xa + m_Xb + m_Xc ;
 float   m_P4x = (m_Xb + m_Xc + m_Xb * (1.0f - k));
        m_P4.x = m_P4x;
        m_P4.y = m_Xa + m_P4x;
        m_P5.x = varisqueezeWidth;
        m_P5.y =  equirectWidth;

m_B0 = new Bezier3(m_P0, m_P1, m_P2);
m_B1 = new Bezier3(m_P3, m_P4, m_P5);
}

public VarisqueezeEquirectCurveBezier(
    int equirectWidth, int varisqueezeWidth, int identityWidth, double smoothness)
{


        // This is a pain to explain without pictures and hand waving.
        // m_Xa, m_Xb and m_Xc are distances from the left side of the equirect
        // image. They help define the various points we need for the curve.
        float k = 1.0f - (float)smoothness;
 m_Xa = (equirectWidth - varisqueezeWidth) / 2;
    m_Xb = (varisqueezeWidth - identityWidth) / 2;
    m_Xc = identityWidth;
        m_P0.x = 0.0f;
        m_P0.y = 0.0f;

float   m_P1x = (m_Xb * k);
        m_P1.x = m_P1x;
        m_P1.y = m_Xa + m_P1x;
        m_P2.x = m_Xb;
        m_P2.y = m_Xa + m_Xb;

        m_P3.x = m_Xb + m_Xc;
        m_P3.y = m_Xa + m_Xb + m_Xc ;
 float   m_P4x = (m_Xb + m_Xc + m_Xb * (1.0f - k));
        m_P4.x = m_P4x;
        m_P4.y = m_Xa + m_P4x;
        m_P5.x = varisqueezeWidth;
        m_P5.y =  equirectWidth;

m_B0 = new Bezier3(m_P0, m_P1, m_P2);
m_B1 = new Bezier3(m_P3, m_P4, m_P5);
}

public float getY(float x)
{
    float result = 0.0f;

    if(x<m_Xb)
    {
        result = getSingleY(m_B0, x);
    }
    else if(x<m_Xb + m_Xc)
    {
        result = m_Xa + x;
    }
    else
    {
        result = getSingleY(m_B1, x);
    }

    return result;
}

public float getX(float y)
{
    float result = 0.0f;

    if(y<m_Xa + m_Xb)
    {
        result = getSingleX(m_B0, y);
    }
    else if(y<m_Xa + m_Xb + m_Xc)
    {
        result = y - m_Xa;
    }
    else
    {
        result = getSingleX(m_B1, y);
    }

    return result;
}

}
