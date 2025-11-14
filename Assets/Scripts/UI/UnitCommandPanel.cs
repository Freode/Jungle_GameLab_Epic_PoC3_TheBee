using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UnitCommandPanel : MonoBehaviour
{
    public static UnitCommandPanel Instance { get; private set; }

    public GameObject panelRoot;
    public RectTransform buttonContainer;
    public GameObject commandButtonPrefab; // prefab with Button + Image + Text

    private UnitAgent currentAgent;

    // a list of default SOCommands, assign in inspector
    public SOCommand[] defaultCommands;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Hide();
    }

    public void Show(UnitAgent agent)
    {
        currentAgent = agent;
        if (panelRoot != null) panelRoot.SetActive(true);
        RebuildCommands();
    }

    public void Hide()
    {
        currentAgent = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void RebuildCommands()
    {
        // clear existing
        foreach (Transform t in buttonContainer) Destroy(t.gameObject);

        if (currentAgent == null) return;

        // get commands from provider (RoleAssigner or other)
        List<ICommand> commands = new List<ICommand>();
        var provider = currentAgent.GetComponent<RoleAssigner>();
        if (provider != null)
        {
            // placeholder: RoleAssigner could expose commands in future
        }

        // fallback to defaultCommands
        if ((commands == null || commands.Count == 0) && defaultCommands != null)
        {
            foreach (var c in defaultCommands)
            {
                commands.Add(c);
            }
        }

        // create buttons
        foreach (var cmd in commands)
        {
            var btnObj = Instantiate(commandButtonPrefab, buttonContainer);
            var btn = btnObj.GetComponentInChildren<Button>();
            var img = btnObj.GetComponentInChildren<UnityEngine.UI.Image>();
            var txt = btnObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (img != null) img.sprite = cmd.Icon;
            if (txt != null) txt.text = cmd.DisplayName;
            btn.onClick.AddListener(() => OnCommandClicked(cmd));
        }
    }

    void OnCommandClicked(ICommand cmd)
    {
        if (currentAgent == null) return;
        if (!cmd.IsAvailable(currentAgent)) return;

        if (cmd.RequiresTarget)
        {
            // enter target selection mode
            TileClickMover.Instance?.EnterMoveMode();
            // store pending command somewhere: for simplicity use TileClickMover's moveMode to mean pending move command
            PendingCommandHolder.Instance?.SetPendingCommand(cmd, currentAgent);
        }
        else
        {
            cmd.Execute(currentAgent, new CommandTarget { type = CommandTargetType.None });
        }
    }
}
