using LanguageExt;
using static LanguageExt.Prelude;

namespace GitLabSymbolServerProxy;

/// <summary>
/// Extension methods for Language-Ext.
/// </summary>
public static class LanguageExtExtensions {
	/// <summary>
	/// Given a collection of tries, boils it down to a
	/// single try, combining all tries into either one
	/// that contains either an enumerable of the try
	/// results, or an AggregateException of all exceptions.
	/// </summary>
	/// <typeparam name="T">Inner T object type.</typeparam>
	/// <param name="tries">All tries.</param>
	/// <returns>Combined tries.</returns>
	public static Try<IEnumerable<T>> Aggregate<T>(this IEnumerable<Try<T>> tries) =>
		tries.Fold(Try<IEnumerable<T>>(List<T>()), (state, newItem) =>
			newItem.Match(
				Succ: itemValue =>
					state.Match(
						Succ: stateValues => Try(stateValues.Concat(Array(itemValue))),
						Fail: exception => state
					),
				Fail: itemException =>
					state.Match(
						Succ: currentTags => Try<IEnumerable<T>>(itemException),
						// Add exception to aggregate exception
						Fail: stateException =>
							Try<IEnumerable<T>>(
								stateException.Match<AggregateException>()
									.With<AggregateException>(stateAggregateException =>
										new AggregateException(stateAggregateException.InnerExceptions.Concat(new[] { itemException }))
									).Otherwise(stateSingleException =>
										new AggregateException(itemException, stateSingleException)
									)
							)
					)
				)
			);
}
