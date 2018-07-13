using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Bezier code from Bob ported to C#

public class Bezier3   {


public Vector2 m_P0;
public Vector2 m_P1;
public Vector2 m_P2;

public Bezier3()
{

}
   


public	void SetBezier(Vector2 p0, Vector2 p1, Vector2 p2 )
 {
	 	m_P0=p0;
		m_P1=p1;
		m_P2 = p2;
 }

public	 Bezier3(Vector2 p0, Vector2 p1, Vector2 p2 )
 {
	 	m_P0=p0;
		m_P1=p1;
		m_P2 = p2;
 }


 public   Vector2  getCoordinates(float t)
    {
    Vector2   result = ((1.0f - t) * ((1.0f - t) * m_P0 + t * m_P1) + t * ((1.0f - t) * m_P1 + t * m_P2));

    return result;
}

public List<float>   getTFromX(float x)
{
    return getT(x, 0);
}

public List<float>  getTFromY(float y)
{
    return getT(y, 1);
}

List<float>  getT(float v, int index)
{
    // Solve the quadratic necessary to get t values corresponding to X or Y
float   a = (m_P0[index] - 2.0f* m_P1[index] + m_P2[index]);
float   b = (-2.0f * m_P0[index] + 2.0f * m_P1[index]);
float   c = (m_P0[index] - v);

float   d = (b * b - 4 * a * c);

List<float> results = new List<float>();


    if(a == 0.0f)
    {
        // linear, not quadratic
        // result.push_back(-c / b);
		results.Add(-c / b);
    }
    else if(d< 0.0f)
    {
        // No real solutions
    }
    else if(d == 0.0f)
    {
        // result.push_back(-b / (2.0 * a));
		results.Add(-b / (2.0f * a));
    }
    else
    {
        float  e = ( Mathf.Sqrt(d));
		// result.push_back((-b + e) / (2.0f * a));
        results.Add((-b + e) / (2.0f * a));
		 results.Add((-b - e) / (2.0f * a));

    }
//  Debug.Log("getT: " + results.Count);
    return results;
}
 
}
