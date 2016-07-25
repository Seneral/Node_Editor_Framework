
using System.Collections.Generic;

public class DialogBlackboard
{
    private static Dictionary<EDialogMultiChoiceVariables, float> _variableValueHolder =
        new Dictionary<EDialogMultiChoiceVariables, float>();


    public enum EDialogMultiChoiceVariables
    {
        Random,
        Test1MultiPath,
        Test2MultiPath,
        TryingThisToo,
    }

    public static float GetValueFor(EDialogMultiChoiceVariables valueToTest)
    {
        return _variableValueHolder[valueToTest];
    }

    public static void SetValue(EDialogMultiChoiceVariables enumValue, float value)
    {
        if (_variableValueHolder.ContainsKey(enumValue))
        {
            _variableValueHolder[enumValue] = value;
        }
        else
        {
            _variableValueHolder.Add(enumValue, value);
        }
    }
}
