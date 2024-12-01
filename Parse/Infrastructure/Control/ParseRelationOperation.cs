using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Data;

namespace Parse.Infrastructure.Control
{
    public class ParseRelationOperation : IParseFieldOperation
    {
        IList<string> Additions { get; }

        IList<string> Removals { get; }

        IParseObjectClassController ClassController { get; }

        ParseRelationOperation(IParseObjectClassController classController) => ClassController = classController;

        ParseRelationOperation(IParseObjectClassController classController, IEnumerable<string> adds, IEnumerable<string> removes, string targetClassName) : this(classController)
        {
            TargetClassName = targetClassName;
            Additions = new ReadOnlyCollection<string>(adds.ToList());
            Removals = new ReadOnlyCollection<string>(removes.ToList());
        }

        public ParseRelationOperation(IParseObjectClassController classController, IEnumerable<ParseObject> adds, IEnumerable<ParseObject> removes) : this(classController)
        {
            adds ??= new ParseObject[0];
            removes ??= new ParseObject[0];

            TargetClassName = adds.Concat(removes).Select(entity => entity.ClassName).FirstOrDefault();
            Additions = new ReadOnlyCollection<string>(GetIdsFromObjects(adds).ToList());
            Removals = new ReadOnlyCollection<string>(GetIdsFromObjects(removes).ToList());
        }

        public object Encode(IServiceHub serviceHub)
        {
            List<object> additions = Additions.Select(id => PointerOrLocalIdEncoder.Instance.Encode(ClassController.CreateObjectWithoutData(TargetClassName, id, serviceHub), serviceHub)).ToList(), removals = Removals.Select(id => PointerOrLocalIdEncoder.Instance.Encode(ClassController.CreateObjectWithoutData(TargetClassName, id, serviceHub), serviceHub)).ToList();

            Dictionary<string, object> addition = additions.Count == 0 ? default : new Dictionary<string, object>
            {
                ["__op"] = "AddRelation",
                ["objects"] = additions
            };

            Dictionary<string, object> removal = removals.Count == 0 ? default : new Dictionary<string, object>
            {
                ["__op"] = "RemoveRelation",
                ["objects"] = removals
            };

            if (addition is { } && removal is { })
            {
                return new Dictionary<string, object>
                {
                    ["__op"] = "Batch",
                    ["ops"] = new[] { addition, removal }
                };
            }
            return addition ?? removal;
        }

        public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous)
        {
            return previous switch
            {
                null => this,
                ParseDeleteOperation { } => throw new InvalidOperationException("You can't modify a relation after deleting it."),
                ParseRelationOperation { } other when other.TargetClassName != TargetClassName => throw new InvalidOperationException($"Related object must be of class {other.TargetClassName}, but {TargetClassName} was passed in."),
                ParseRelationOperation { ClassController: var classController } other => new ParseRelationOperation(classController, Additions.Union(other.Additions.Except(Removals)).ToList(), Removals.Union(other.Removals.Except(Additions)).ToList(), TargetClassName),
                _ => throw new InvalidOperationException("Operation is invalid after previous operation.")
            };
        }

        public object Apply(object oldValue, string key)
        {
            return oldValue switch
            {
                _ when Additions.Count == 0 && Removals.Count == 0 => default,
                null => ClassController.CreateRelation(null, key, TargetClassName),
                ParseRelationBase { TargetClassName: { } oldClassname } when oldClassname != TargetClassName => throw new InvalidOperationException($"Related object must be a {oldClassname}, but a {TargetClassName} was passed in."),
                ParseRelationBase { } oldRelation => (Relation: oldRelation, oldRelation.TargetClassName = TargetClassName).Relation,
                _ => throw new InvalidOperationException("Operation is invalid after previous operation.")
            };
        }

        public string TargetClassName { get; }

        IEnumerable<string> GetIdsFromObjects(IEnumerable<ParseObject> objects)
        {
            foreach (ParseObject entity in objects)
            {
                if (entity.ObjectId is null)
                {
                    throw new ArgumentException("You can't add an unsaved ParseObject to a relation.");
                }

                if (entity.ClassName != TargetClassName)
                {
                    throw new ArgumentException($"Tried to create a ParseRelation with 2 different types: {TargetClassName} and {entity.ClassName}");
                }
            }

            return objects.Select(entity => entity.ObjectId).Distinct();
        }
    }
}
