namespace ConvNetSharp.Layers
{
    public interface ILastLayer
    {
        double Backward(double y);

        double Backward(double y,int i);

        double Backward(double[] y);
    }
}