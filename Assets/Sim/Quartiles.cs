using UnityEngine;
using System.Collections;

namespace Stats.Quartiles
{

public class Quartiles {

        public double umidmean( double[] arr ){

            if (arr.Length < 6) {
                Debug.Log("too short");
            }

            int len = arr.Length;

            double delta,mean = 0f;

            int n = 0;

            int low = 0;

            int high = 0;

            // Quartiles sit between values
            if (len % 8 == 0) {
                low = Mathf.RoundToInt(len * 0.625f);
                high = Mathf.RoundToInt(len * 0.875f) - 1;
            }
            else {
                low = (int) Mathf.Ceil(len * 0.625f);
                high = (int) Mathf.Floor(len * 0.875f) - 1;
            }

            // Compute an arithmetic mean...
            for (int i = low; i <= high; i++) {
                n += 1;
                delta = arr[i] - mean;
                mean += delta / n;
            }

            return mean;
        }



        public double lmidmean ( double[] arr ){

            if (arr.Length < 6) {
                Debug.Log("too short");
            }

            int len = arr.Length;

            double delta,mean = 0f;

            int n = 0;

            int low = 0;

            int high = 0;

    // Quartiles sit between values
    if ( len % 8 == 0 ) {
        low = Mathf.RoundToInt(len*0.125f);
        high = Mathf.RoundToInt(len*0.375f) - 1;
    }
    else {
        low = (int) Mathf.Ceil( len*0.125f );
        high = (int) Mathf.Floor( len*0.375f ) - 1;
    }

    // Compute an arithmetic mean...
    for ( int i= low; i <= high; i++ ) {
        n += 1;
        delta = arr[ i ] - mean;
        mean += delta / n;
    }

    return mean;
}

}


}