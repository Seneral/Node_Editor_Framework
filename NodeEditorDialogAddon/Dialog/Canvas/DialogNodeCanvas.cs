using System.Collections.Generic;
using System.Linq;
using NodeEditorFramework;
using UnityEngine;

[NodeCanvasType("Dialog Canvas")]
public class DialogNodeCanvas : NodeCanvas
{
    public string TestNewData = "Superman";
    public float age = 33;

    [SerializeField]
    private List<DialogStartNode> _lstDialogStartNodes = new List<DialogStartNode>();

    private Dictionary<int, BaseDialogNode> _lstActiveDialogs = new Dictionary<int, BaseDialogNode>();

    public bool HasDialogWithId(int dialogIdToLoad)
    {
        DialogStartNode node = _lstDialogStartNodes.FirstOrDefault(x => x.DialogID == dialogIdToLoad);
        return node != default(DialogStartNode);
    }


    public override void BeforeSavingCanvas()
    {
        _lstDialogStartNodes.Clear();
        foreach (Node node in nodes)
        {
            if (node is DialogStartNode)
            {
                _lstDialogStartNodes.Add(node as DialogStartNode);
            }
        }
    }

    public IEnumerable<int> GetAllDialogId()
    {
        return _lstDialogStartNodes.Select(startNode => startNode.DialogID).ToList();
    }

    public void ActivateDialog(int dialogIdToLoad, bool goBackToBeginning)
    {
        BaseDialogNode node;
        if (!_lstActiveDialogs.TryGetValue(dialogIdToLoad, out node))
        {
            node = _lstDialogStartNodes.First(x => x.DialogID == dialogIdToLoad);
            _lstActiveDialogs.Add(dialogIdToLoad, node);
        }
        else
        {
            if (goBackToBeginning && !(node is DialogStartNode))
            {
                _lstActiveDialogs[dialogIdToLoad] = _lstDialogStartNodes.First(x => x.DialogID == dialogIdToLoad);
            }
        }
    }

    public BaseDialogNode GetDialog(int dialogIdToLoad)
    {
        BaseDialogNode node;
        if (!_lstActiveDialogs.TryGetValue(dialogIdToLoad, out node))
        {
            ActivateDialog(dialogIdToLoad, false);
        }
        return _lstActiveDialogs[dialogIdToLoad];
    }

    public void InputToDialog(int dialogIdToLoad, int inputValue)
    {
        BaseDialogNode node;
        if (_lstActiveDialogs.TryGetValue(dialogIdToLoad, out node))
        {
            node = node.Input(inputValue);
            if(node != null)
                node = node.PassAhead(inputValue);
            _lstActiveDialogs[dialogIdToLoad] = node;
        }
    }
}
