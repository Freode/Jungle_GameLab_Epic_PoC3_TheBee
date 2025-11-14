using System.Collections.Generic;

public interface IUnitCommandProvider
{
    IEnumerable<ICommand> GetCommands(UnitAgent agent);
}
