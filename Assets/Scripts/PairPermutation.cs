using System;
using System.Collections.Generic;
using UnityEngine;

public class PairPermutation
{
    public static System.Collections.Generic.IEnumerable<int[]> AllFor(int[] array)
    {
        if (array == null || array.Length == 0)
        {
            yield return new int[0];
        }
        else
        {
            for (int pick = 0; pick < array.Length; ++pick)
            {
                int item = array[pick];
                int i = -1;
                int[] rest = System.Array.FindAll<int>(
                    array, delegate (int p) { return ++i != pick; }
                );
                foreach (int[] restPermuted in AllFor(rest))
                {
                    i = -1;
                    yield return System.Array.ConvertAll<int, int>(
                        array,
                        delegate (int p) {
                            return ++i == 0 ? item : restPermuted[i - 1];
                        }
                    );
                }
            }
        }
    }

    public static List<int[]> getPermutation(int[] list)
    {
        List<int[]> permutations = new List<int[]>(); 
        foreach (int[] permutation in PairPermutation.AllFor(list))
        {
            //string pair = string.Join(" ", permutation);
            //Debug.Log(pair);
            permutations.Add(permutation);
            
        }
        return permutations;
    }

    //usage
    //public static void main()
    //{
    //    PairPermutation formPermute = new PairPermutation();
    //    List<int[]> permutations = PairPermutation.getPermutation(new int[] { 1, 6, 5, 9 });
    //}
}