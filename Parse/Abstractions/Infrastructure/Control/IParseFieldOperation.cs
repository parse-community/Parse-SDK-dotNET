namespace Parse.Abstractions.Infrastructure.Control
{
    /// <summary>
    /// A ParseFieldOperation represents a modification to a value in a ParseObject.
    /// For example, setting, deleting, or incrementing a value are all different kinds of
    /// ParseFieldOperations. ParseFieldOperations themselves can be considered to be
    /// immutable.
    /// </summary>
    public interface IParseFieldOperation
    {
        /// <summary>
        /// Converts the ParseFieldOperation to a data structure that can be converted to JSON and sent to
        /// Parse as part of a save operation.
        /// </summary>
        /// <returns>An object to be JSONified.</returns>
        object Encode(IServiceHub serviceHub);

        /// <summary>
        /// Returns a field operation that is composed of a previous operation followed by
        /// this operation. This will not mutate either operation. However, it may return
        /// <code>this</code> if the current operation is not affected by previous changes.
        /// For example:
        ///   {increment by 2}.MergeWithPrevious({set to 5})       -> {set to 7}
        ///         {set to 5}.MergeWithPrevious({increment by 2}) -> {set to 5}
        ///        {add "foo"}.MergeWithPrevious({delete})         -> {set to ["foo"]}
        ///           {delete}.MergeWithPrevious({add "foo"})      -> {delete}        /// </summary>
        /// <param name="previous">The most recent operation on the field, or null if none.</param>
        /// <returns>A new ParseFieldOperation or this.</returns>
        IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous);

        /// <summary>
        /// Returns a new estimated value based on a previous value and this operation. This
        /// value is not intended to be sent to Parse, but it is used locally on the client to
        /// inspect the most likely current value for a field.
        ///
        /// The key and object are used solely for ParseRelation to be able to construct objects
        /// that refer back to their parents.
        /// </summary>
        /// <param name="oldValue">The previous value for the field.</param>
        /// <param name="key">The key that this value is for.</param>
        /// <returns>The new value for the field.</returns>
        object Apply(object oldValue, string key);
    }
}
