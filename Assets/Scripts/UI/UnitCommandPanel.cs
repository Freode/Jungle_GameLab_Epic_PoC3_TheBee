using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

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

        // get commands from provider (IUnitCommandProvider) if available
        List<ICommand> commands = new List<ICommand>();
        var provider = currentAgent.GetComponent<IUnitCommandProvider>();
        if (provider != null)
        {
            foreach (var c in provider.GetCommands(currentAgent))
            {
                if (c != null) commands.Add(c);
            }
        }

        // fallback to defaultCommands if none provided
        if (commands.Count == 0 && defaultCommands != null)
        {
            foreach (var c in defaultCommands)
            {
                commands.Add(c);
            }
        }

        // create buttons (left-to-right via HorizontalLayoutGroup on buttonContainer)
        foreach (var cmd in commands)
        {
            var command = cmd; // local copy to avoid closure capture issues
            var btnObj = Instantiate(commandButtonPrefab, buttonContainer);
            btnObj.name = "cmd_" + command.Id;
            var btn = btnObj.GetComponentInChildren<Button>();
            var img = btnObj.GetComponentInChildren<UnityEngine.UI.Image>();
            var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            var txt = btnObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (img != null) img.sprite = command.Icon;
            if (tmp != null) tmp.text = command.DisplayName;
            else if (txt != null) txt.text = command.DisplayName;

            // disable if not available
            bool avail = command.IsAvailable(currentAgent);
            if (btn != null)
            {
                btn.interactable = avail;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnCommandClicked(command));
            }
        }
    }

    void OnCommandClicked(ICommand cmd)
    {
        if (currentAgent == null) return;
        if (!cmd.IsAvailable(currentAgent)) return;

        // Special-case: construct_hive should execute immediately at the agent's current tile
        if (cmd.Id == "construct_hive")
        {
            cmd.Execute(currentAgent, CommandTarget.ForTile(currentAgent.q, currentAgent.r));
            return;
        }

        if (cmd.RequiresTarget)
        {
            // ensure PendingCommandHolder exists
            PendingCommandHolder.EnsureInstance();
            if (PendingCommandHolder.Instance == null)
            {
                Debug.LogWarning("PendingCommandHolder not found in scene. Command will not be queued.");
                return;
            }

            // enter target selection mode
            TileClickMover.Instance?.EnterMoveMode();
            PendingCommandHolder.Instance?.SetPendingCommand(cmd, currentAgent);
        }
        else
        {
            cmd.Execute(currentAgent, new CommandTarget { type = CommandTargetType.None });
        }
    }
}
