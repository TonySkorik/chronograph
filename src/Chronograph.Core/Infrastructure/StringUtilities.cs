using System.Text;

namespace Chronograph.Core.Infrastructure;

/// <summary>
/// Contains utility methods used on strings.
/// </summary>
internal static class StringUtilities
{
	/// <summary>
	/// Lowercases the first char of the specified actionDescription if it is not already lower case. 
	/// </summary>
	/// <param name="actionDescription">The string to prepare.</param>
	public static string LowercaseFirstChar(this string actionDescription)
	{
		if (string.IsNullOrEmpty(actionDescription))
		{
			return string.Empty;
		}

		if (!char.IsUpper(actionDescription[0]))
		{
			return actionDescription;
		}

		var ret = string.Create(
			actionDescription.Length,
			actionDescription,
			(span, unupperedString) =>
			{
				for (var i = 0; i < unupperedString.Length; i++)
				{
					span[i] = i == 0
						? char.ToLower(unupperedString[i])
						: unupperedString[i];
				}
			}
		);

		return ret;
	}
	
	 /// <summary>
    /// Escape curly braces in action description to prevent it from being interpreted as a format string.
    /// Curly braces may appear in action description for example from records being present in string interpolation.
    /// </summary>
    public static string EscapeCurlyBraces(this string target)
    {
        if (!target.Contains("{"))
        { 
            // no opening curly brace means nothing to escape
            return target;
        }

        // curly braces should be escaped only if there are spaces between them
        // to prevent escaping of structured logging parameter placeholders like {SomeParameter}

        StringBuilder escaped = new StringBuilder();

        var i = 0;
        
        while (i < target.Length)
        {
            var currentChar = target[i];
            
            if (currentChar != '{' && currentChar != '}')
            {
                // add non-braces and closing braces as-is
                
                escaped.Append(currentChar);
                i++;
                
                continue;
            }

            if (currentChar == '{')
            {
                int unbalancedBracesCount = 1;
                
                var j = i+1;
                
                // search for closing curly brace
                while (unbalancedBracesCount > 0)
                {
                    if (j >= target.Length)
                    { 
                        // means unbalanced braces found - escape every brace til the end of the string
                        var substringBetweenUnbalancedBraces = target[i..];

                        var escapedSubstring = substringBetweenUnbalancedBraces
                            .Replace("{", "{{")
                            .Replace("}", "}}");

                        escaped.Append(escapedSubstring);

                        i = j;
                        
                        break;
                    }

                    var middleChar = target[j];

                    if (middleChar == '{')
                    {
                        unbalancedBracesCount++;
                    }

                    if (middleChar == '}')
                    {
                        unbalancedBracesCount--;
                    }

                    // means middle char is not a brace
                    j++;
                }

                // search for a space symbol between the i and j
                // if not found - don't escape anything
                // if found - escape all the braces between the i and j
                
                var substringBetweenBraces = target[i..j];

                if (substringBetweenBraces.Contains(" "))
                {
                    var escapedSubstring = substringBetweenBraces
                        .Replace("{", "{{")
                        .Replace("}", "}}");

                    escaped.Append(escapedSubstring);
                }
                else
                { 
                    // no space - append as is
                    escaped.Append(substringBetweenBraces);
                }

                i = j;
            }
        }

        return escaped.ToString();
    }
}
