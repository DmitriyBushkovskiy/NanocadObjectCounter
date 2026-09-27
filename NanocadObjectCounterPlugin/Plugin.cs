using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.Runtime;
using Teigha.DatabaseServices;

namespace NanocadObjectCounterPlugin;
public class Plugin
{
    [CommandMethod("CountObjects")]
    public void CountObjects()
    {
        var document = Application.DocumentManager.MdiActiveDocument;
        var editor = document.Editor;
        var db = document.Database;

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var options = new PromptEntityOptions("Выберите примитив. Нажмите \"Enter\", если нужно посчитать количество всех примитивов на чертеже:");
            
            var res = editor.GetEntity(options);
            var promptSelectionResult = editor.SelectAll();
            if (res is { Status: PromptStatus.OK })
            {
                var objectId = res.ObjectId;
                var selectedObject = objectId.GetObject(OpenMode.ForRead);

                var filter = new SelectionFilter(new[]
                {
                    new TypedValue((int)DxfCode.Start, RXObject.GetClass(selectedObject.GetType()).DxfName)
                });
                promptSelectionResult = editor.SelectAll(filter);
            }
            
            var selectionSet = promptSelectionResult.Status == PromptStatus.OK 
                ? promptSelectionResult.Value 
                : new SelectionSet();
            
            if (selectionSet.Count == 0)
                editor.WriteMessage("На чертеже не найдено ни одного объекта");

            var objectCounts = new Dictionary<Type, int>();
            
            foreach (SelectedObject selectedObject in selectionSet)
            {
                var dbObject = tr.GetObject(selectedObject.ObjectId, OpenMode.ForRead);
                var type = dbObject.GetType();
                if (!objectCounts.TryAdd(type, 1))
                    objectCounts[type]++;
            }

            if (objectCounts.Count == 1)
            {
                editor.WriteMessage($"Найдено объектов типа {objectCounts.First().Key.Name}: {objectCounts.First().Value} шт.");
                return;
            }
            
            editor.WriteMessage($"Найдено объектов: {objectCounts.Values.Sum()} шт. Следующих типов:");
            foreach (var pair in objectCounts)
            {
                editor.WriteMessage($"{pair.Key.Name}: {pair.Value} шт.");
            }
        }
    }
}
