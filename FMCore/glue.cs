using Godot;
using System;

// Helper classes and other C# to GDScript glue go here

public class AutoLoadHelper<T> : Node
{
    public static T Get(string autoload, string propertyName)
    {
        SceneTree tree = (SceneTree) Engine.GetMainLoop();
        Node root = tree.EditedSceneRoot;
        return (T) root.GetNode(autoload).Get(propertyName);
    }
}

public class AutoLoadHelper : Node 
{
    public static Node GetNode(string autoload)
    {
        SceneTree tree = (SceneTree) Engine.GetMainLoop();
        Node root = tree.EditedSceneRoot;
        return root.GetNode(autoload);
    }

}

public static class GDSFmFuncs
{
    //Godot easing func.
    static double Ease(double p_x, double p_c) {
        if (p_x < 0.0)
            p_x = 0.0f;
        else if (p_x > 1.0)
            p_x = 1.0f;
        if (p_c > 0.0) {
            if (p_c < 1.0) {
                return 1.0 - Math.Pow(1.0 - p_x, 1.0 / p_c);
            } else {
                return Math.Pow(p_x, p_c);
            }
        } else if (p_c < 0.0) {
            //inout ease

            if (p_x < 0.5) {
                return Math.Pow(p_x * 2.0, -p_c) * 0.5;
            } else {
                return (1.0 - Math.Pow(1.0 - (p_x - 0.5) * 2.0, -p_c)) * 0.5 + 0.5;
            }
        } else
            return 0.0f; // no ease (raw)
    }

    static double Ease(double first, double last, double percent, double curve=1.0f)
    {
        double amt = last-first;
        return Ease(percent, curve) * amt + first;
    }
}