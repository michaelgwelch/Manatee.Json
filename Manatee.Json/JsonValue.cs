using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Manatee.Json.Internal;
using Manatee.Json.Parsing;

namespace Manatee.Json
{
	/// <summary>
	/// Represents a JSON value.
	/// </summary>
	/// <remarks>
	/// A value can consist of a string, a numerical value, a boolean (true or false), a null
	/// placeholder, a JSON array of values, or a nested JSON object.
	/// </remarks>
	public class JsonValue : IEquatable<JsonValue>
	{
		private struct NumberValue : IEquatable<NumberValue>
		{
			// By default we have a double with value 0
			// because _isFloat is false
			private readonly float _floatValue;
			private readonly double _doubleValue;
			private readonly bool _isFloat;

			public bool IsFloat => _isFloat;

			public NumberValue(double d)
			{
				_floatValue = 0;
				_doubleValue = d;
				_isFloat = false;
			}

			public NumberValue(float f)
			{
				_floatValue = f;
				_doubleValue = 0;
				_isFloat = true;
			}

			public static implicit operator NumberValue(float f)
			{
				return new NumberValue(f);
			}

			public static implicit operator NumberValue(double d)
			{
				return new NumberValue(d);
			}

			public override int GetHashCode()
			{
				if (_isFloat)
				{
					return _floatValue.GetHashCode();
				}
				return _doubleValue.GetHashCode();
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(obj, null)) return false;
				if (!(obj is NumberValue)) return false;
				return Equals((NumberValue)obj);
			}

			public bool Equals(NumberValue other)
			{
				return _floatValue.Equals(other._floatValue)
					&& _doubleValue.Equals(other._doubleValue)
					&& _isFloat.Equals(other._isFloat);
			}

			public void AppendString(StringBuilder builder)
			{
				if (_isFloat)
				{
					builder.AppendFormat(CultureInfo.InvariantCulture, "{0:G9}", _floatValue);
				}
				else
				{
					builder.AppendFormat(CultureInfo.InvariantCulture, "{0:G17}", _doubleValue);
				}
			}

			public double Number => _isFloat ? _floatValue : _doubleValue;

			public float Float => _isFloat ? _floatValue : (float)_doubleValue;
		}




		private readonly bool _boolValue = default!;
		private readonly string _stringValue = default!;
		private readonly NumberValue _numberValue = default!;
		private readonly JsonObject _objectValue = default!;
		private readonly JsonArray _arrayValue = default!;

		/// <summary>
		/// Globally defined null-valued JSON value.
		/// </summary>
		/// <remarks>
		/// When adding values to a <see cref="JsonObject"/> or <see cref="JsonArray"/>, nulls will automatically be converted into this field.
		/// </remarks>
#pragma warning disable 618
		public static readonly JsonValue Null = new JsonValue();
#pragma warning restore 618

		/// <summary>
		/// Accesses the <see cref="JsonValue"/> as a boolean.
		/// </summary>
		/// <exception cref="JsonValueIncorrectTypeException">
		/// Thrown when this <see cref="JsonValue"/> does not contain a boolean.
		/// </exception>
		public bool Boolean
		{
			get
			{
				if (Type != JsonValueType.Boolean && JsonOptions.ThrowOnIncorrectTypeAccess)
					throw new JsonValueIncorrectTypeException(Type, JsonValueType.Boolean);
				return _boolValue;
			}
		}
		/// <summary>
		/// Accesses the <see cref="JsonValue"/> as a string.
		/// </summary>
		/// <exception cref="JsonValueIncorrectTypeException">
		/// Thrown when this <see cref="JsonValue"/> does not contain a string.
		/// </exception>
		/// <remarks>
		/// Setting the value as a string will automatically change the <see cref="JsonValue"/>'s type and discard the old data.
		/// </remarks>
		public string String
		{
			get
			{
				if (Type != JsonValueType.String && JsonOptions.ThrowOnIncorrectTypeAccess)
					throw new JsonValueIncorrectTypeException(Type, JsonValueType.String);
				return _stringValue;
			}
		}
		/// <summary>
		/// Accesses the <see cref="JsonValue"/> as a numeric value.
		/// </summary>
		/// <exception cref="JsonValueIncorrectTypeException">
		/// Thrown when this <see cref="JsonValue"/> does not contain a numeric value.
		/// </exception>
		public double Number
		{
			get
			{
				if (Type != JsonValueType.Number && JsonOptions.ThrowOnIncorrectTypeAccess)
					throw new JsonValueIncorrectTypeException(Type, JsonValueType.Number);
				return _numberValue.Number;
			}
		}

		/// <summary>
		/// Access the <see cref="JsonValue"/> as a float value. This property only works
		/// as intended if the original JsonValue was constructed from a float. If it was
		/// constructed froma double that double will be cast to a float.
		/// </summary>
		public float Float
		{
			get
			{
				{
					if (Type != JsonValueType.Number && _numberValue.IsFloat && JsonOptions.ThrowOnIncorrectTypeAccess)
						throw new JsonValueIncorrectTypeException(Type, JsonValueType.Number);
					return _numberValue.Float;
				}
			}
		}
		/// <summary>
		/// Accesses the <see cref="JsonValue"/> as a JSON object.
		/// </summary>
		/// <exception cref="JsonValueIncorrectTypeException">
		/// Thrown when this <see cref="JsonValue"/> does not contain a Json object.
		/// </exception>
		public JsonObject Object
		{
			get
			{
				if (Type != JsonValueType.Object && JsonOptions.ThrowOnIncorrectTypeAccess)
					throw new JsonValueIncorrectTypeException(Type, JsonValueType.Object);
				return _objectValue;
			}
		}
		/// <summary>
		/// Accesses the <see cref="JsonValue"/> as a JSON array.
		/// </summary>
		/// <exception cref="JsonValueIncorrectTypeException">
		/// Thrown when this <see cref="JsonValue"/> does not contain a Json array.
		/// </exception>
		public JsonArray Array
		{
			get
			{
				if (Type != JsonValueType.Array && JsonOptions.ThrowOnIncorrectTypeAccess)
					throw new JsonValueIncorrectTypeException(Type, JsonValueType.Array);
				return _arrayValue;
			}
		}
		/// <summary>
		/// Gets the value type of the existing data.
		/// </summary>
		public JsonValueType Type { get; }

		/// <summary>
		/// Creates a null <see cref="JsonValue"/>.
		/// </summary>
		public JsonValue()
		{
			Type = JsonValueType.Null;
		}
		/// <summary>
		/// Creates a <see cref="JsonValue"/> from a boolean.
		/// </summary>
		public JsonValue(bool b)
		{
			_boolValue = b;
			Type = JsonValueType.Boolean;
		}
		/// <summary>
		/// Creates a <see cref="JsonValue"/> from a string.
		/// </summary>
		public JsonValue(string s)
		{
			_stringValue = s ?? throw new ArgumentNullException(nameof(s));
			Type = JsonValueType.String;
		}

		#region Integer Constructors
		// Now that we explicitly support float, we need all of these constructors
		// to ensure all non-float numeric types are still considered doubles just
		// like they would have been prior to the addition of float.
		public JsonValue(sbyte s) : this((double)s) { }
		public JsonValue(byte b) : this((double)b) { }
		public JsonValue(short s) : this((double)s) { }
		public JsonValue(ushort u) : this((double)u) { }
		public JsonValue(int i) : this((double)i) { }
		public JsonValue(uint u) : this((double)u) { }
		public JsonValue(long l) : this((double)l) { }
		public JsonValue(ulong u) : this((double)u) { }

		#endregion

		/// <summary>
		/// Creates a <see cref="JsonValue"/> from a float
		///
		/// The JsonValue will remember that this is a float and use that information
		/// to serialize it differently than a double.
		/// </summary>
		/// <param name="f"></param>
		public JsonValue(float f)
		{
			_numberValue = f;
			Type = JsonValueType.Number;
		}

		/// <summary>
		/// Creates a <see cref="JsonValue"/> from a numeric value.
		/// </summary>
		public JsonValue(double n)
		{
			_numberValue = n;
			Type = JsonValueType.Number;
		}
		/// <summary>
		/// Creates a <see cref="JsonValue"/> from a JSON object.
		/// </summary>
		public JsonValue(JsonObject o)
		{
			_objectValue = o ?? throw new ArgumentNullException(nameof(o));
			Type = JsonValueType.Object;
		}
		/// <summary>
		/// Creates a <see cref="JsonValue"/> from a JSON array.
		/// </summary>
		public JsonValue(JsonArray a)
		{
			_arrayValue = a ?? throw new ArgumentNullException(nameof(a));
			Type = JsonValueType.Array;
		}
		/// <summary>
		/// Creates a copy of a <see cref="JsonValue"/>.
		/// </summary>
		public JsonValue(JsonValue other)
		{
			if (other == null) throw new ArgumentNullException(nameof(other));

			_arrayValue = other._arrayValue;
			_objectValue = other._objectValue;
			_numberValue = other._numberValue;
			_stringValue = other._stringValue;
			_boolValue = other._boolValue;
			Type = other.Type;
		}

		/// <summary>
		/// Creates a string representation of the JSON data.
		/// </summary>
		/// <param name="indentLevel">The indention level for the value.</param>
		/// <returns>A string.</returns>
		public string GetIndentedString(int indentLevel = 0)
		{
			var builder = new StringBuilder();
			AppendIndentedString(builder, indentLevel);
			return builder.ToString();
		}
		internal void AppendIndentedString(StringBuilder builder, int indentLevel)
		{
			switch (Type)
			{
				case JsonValueType.Object:
					_objectValue.AppendIndentedString(builder, indentLevel);
					break;
				case JsonValueType.Array:
					_arrayValue.AppendIndentedString(builder, indentLevel);
					break;
				default:
					AppendString(builder);
					break;
			}
		}

		/// <summary>
		/// Creates a string that represents this <see cref="JsonValue"/>.
		/// </summary>
		/// <returns>A string representation of this <see cref="JsonValue"/>.</returns>
		/// <remarks>
		/// Passing the returned string back into the parser will result in a copy of this <see cref="JsonValue"/>.
		/// </remarks>
		public override string ToString()
		{
			var stringBuilder = new StringBuilder();
			AppendString(stringBuilder);
			return stringBuilder.ToString();
		}
		internal void AppendString(StringBuilder builder)
		{
			switch (Type)
			{
				case JsonValueType.Number:
					_numberValue.AppendString(builder);
					break;
				case JsonValueType.String:
					builder.Append('"');
					_stringValue.InsertEscapeSequences(builder);
					builder.Append('"');
					break;
				case JsonValueType.Boolean:
					builder.Append(_boolValue ? "true" : "false");
					break;
				case JsonValueType.Object:
					_objectValue.AppendString(builder);
					break;
				case JsonValueType.Array:
					_arrayValue.AppendString(builder);
					break;
				default:
					builder.Append("null");
					break;
			}
		}
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>. </param>
		public override bool Equals(object? obj)
		{
			return Equals(obj?.AsJsonValue());
		}
		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
		/// <param name="other">An object to compare with this object.</param>
		public bool Equals(JsonValue? other)
		{
			// using a == here would result in recursion and death by stack overflow
			if (ReferenceEquals(other, null)) return false;
			if (other.Type != Type) return false;
			switch (Type)
			{
				case JsonValueType.Number:
					return _numberValue.Equals(other._numberValue);
				case JsonValueType.String:
					return _stringValue.Equals(other.String);
				case JsonValueType.Boolean:
					return _boolValue.Equals(other.Boolean);
				case JsonValueType.Object:
					return _objectValue.Equals(other.Object);
				case JsonValueType.Array:
					return _arrayValue.Equals(other.Array);
				case JsonValueType.Null:
					return true;
			}
			return false;
		}
		/// <summary>
		/// Serves as a hash function for a particular type. 
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			switch (Type)
			{
				case JsonValueType.Number:
					return _numberValue.GetHashCode();
				case JsonValueType.String:
					return _stringValue.GetHashCode();
				case JsonValueType.Boolean:
					return _boolValue.GetHashCode();
				case JsonValueType.Object:
					return _objectValue.GetHashCode();
				case JsonValueType.Array:
					return _arrayValue.GetHashCode();
				case JsonValueType.Null:
					return JsonValueType.Null.GetHashCode();
			}
			// ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
			return base.GetHashCode();
		}
		/// <summary>
		/// Parses a <see cref="string"/> containing a JSON value.
		/// </summary>
		/// <param name="source">the <see cref="string"/> to parse.</param>
		/// <returns>The JSON value represented by the <see cref="string"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="source"/> is empty or whitespace.</exception>
		/// <exception cref="JsonSyntaxException">Thrown if <paramref name="source"/> contains invalid JSON syntax.</exception>
		public static JsonValue Parse(string source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (string.IsNullOrWhiteSpace(source))
				throw new ArgumentException("Source string contains no data.");
			return JsonParser.Parse(source);
		}
		/// <summary>
		/// Parses data from a <see cref="StreamReader"/> containing a JSON value.
		/// </summary>
		/// <param name="stream">the <see cref="StreamReader"/> to parse.</param>
		/// <returns>The JSON value represented by the <see cref="string"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="stream"/> is at the end.</exception>
		/// <exception cref="JsonSyntaxException">Thrown if <paramref name="stream"/> contains invalid JSON syntax.</exception>
		public static JsonValue Parse(TextReader stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			return JsonParser.Parse(stream);
		}
		/// <summary>
		/// Parses data from a <see cref="StreamReader"/> containing a JSON value.
		/// </summary>
		/// <param name="stream">the <see cref="StreamReader"/> to parse.</param>
		/// <returns>The JSON value represented by the <see cref="string"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="stream"/> is at the end.</exception>
		/// <exception cref="JsonSyntaxException">Thrown if <paramref name="stream"/> contains invalid JSON syntax.</exception>
		public static Task<JsonValue> ParseAsync(TextReader stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			return JsonParser.ParseAsync(stream);
		}

#pragma warning disable CS8603 // Possible null reference return.
		/// <summary>
		/// Implicitly converts a <see cref="bool"/> into a <see cref="JsonValue"/>.
		/// </summary>
		/// <param name="b">A <see cref="bool"/>.</param>
		/// <returns>A <see cref="JsonValue"/> that represents the <see cref="bool"/>.</returns>
		/// <remarks>
		/// This is useful when creating an initialized <see cref="JsonObject"/> or <see cref="JsonArray"/>.
		/// </remarks>
		/// <example>
		/// ```
		/// JsonObject obj = new JsonObject{
		///		{"stringData", "string"},
		///		{"numberData", 10.6},
		///		{"boolData", true},
		///		{"arrayData", new JsonArray{false, "Array String", JsonValue.Null, 8e-4}},
		///		{"objectData", new JsonObject{
		///			{"stringData2", "another string"},
		///			{"moreBoolData", false}}}};
		/// ```
		/// </example>
		public static implicit operator JsonValue(bool b)
		{
			return new JsonValue(b);
		}
		/// <summary>
		/// Implicitly converts a <see cref="string"/> into a <see cref="JsonValue"/>.
		/// </summary>
		/// <param name="s">A <see cref="string"/>.</param>
		/// <returns>A <see cref="JsonValue"/> that represents the <see cref="string"/>.</returns>
		/// <remarks>
		/// This is useful when creating an initialized <see cref="JsonObject"/> or <see cref="JsonArray"/>.
		/// </remarks>
		/// <example>
		/// ```
		/// JsonObject obj = new JsonObject{
		///		{"stringData", "string"},
		///		{"numberData", 10.6},
		///		{"boolData", true},
		///		{"arrayData", new JsonArray{false, "Array String", JsonValue.Null, 8e-4}},
		///		{"objectData", new JsonObject{
		///			{"stringData2", "another string"},
		///			{"moreBoolData", false}}}};
		/// ```
		/// </example>
		public static implicit operator JsonValue(string? s)
		{
			return s is null ? null : new JsonValue(s);
		}

		#region Integer Implicit Conversions
		// Now that we explicitly support float, we need all of these implicit operators
		// to ensure all non-float numeric types are still considered doubles, just as they
		// would have been prior to the addition of the float implicit conversion.
		public static implicit operator JsonValue(byte b)
		{
			return new JsonValue(b);
		}

		public static implicit operator JsonValue(sbyte s)
		{
			return new JsonValue(s);
		}

		public static implicit operator JsonValue(short s)
		{
			return new JsonValue(s);
		}

		public static implicit operator JsonValue(ushort u)
		{
			return new JsonValue(u);
		}

		public static implicit operator JsonValue(int i)
		{
			return new JsonValue(i);
		}

		public static implicit operator JsonValue(uint u)
		{
			return new JsonValue(u);
		}

		public static implicit operator JsonValue(long l)
		{
			return new JsonValue(l);
		}

		public static implicit operator JsonValue(ulong u)
		{
			return new JsonValue(u);
		}

		#endregion

		/// <summary>
		/// Implicitly converts a <see cref="float"/> into a <see cref="JsonValue"/>.
		/// </summary>
		/// <param name="n">A <see cref="float"/>.</param>
		/// <returns>A <see cref="JsonValue"/> that represents the <see cref="float"/>.</returns>
		/// <remarks>
		/// This is useful when creating an initialized <see cref="JsonObject"/> or <see cref="JsonArray"/>.
		/// </remarks>
		/// <example>
		/// ```
		/// JsonObject obj = new JsonObject{
		///		{"stringData", "string"},
		///		{"numberData", 10.6f},
		///		{"boolData", true},
		///		{"arrayData", new JsonArray{false, "Array String", JsonValue.Null, 8e-4}},
		///		{"objectData", new JsonObject{
		///			{"stringData2", "another string"},
		///			{"moreBoolData", false}}}};
		/// ```
		/// </example>
		public static implicit operator JsonValue(float n)
		{
			return new JsonValue(n);
		}
		/// <summary>
		/// Implicitly converts a <see cref="double"/> into a <see cref="JsonValue"/>.
		/// </summary>
		/// <param name="n">A <see cref="double"/>.</param>
		/// <returns>A <see cref="JsonValue"/> that represents the <see cref="double"/>.</returns>
		/// <remarks>
		/// This is useful when creating an initialized <see cref="JsonObject"/> or <see cref="JsonArray"/>.
		/// </remarks>
		/// <example>
		/// ```
		/// JsonObject obj = new JsonObject{
		///		{"stringData", "string"},
		///		{"numberData", 10.6},
		///		{"boolData", true},
		///		{"arrayData", new JsonArray{false, "Array String", JsonValue.Null, 8e-4}},
		///		{"objectData", new JsonObject{
		///			{"stringData2", "another string"},
		///			{"moreBoolData", false}}}};
		/// ```
		/// </example>
		public static implicit operator JsonValue(double n)
		{
			return new JsonValue(n);
		}
		/// <summary>
		/// Implicitly converts a <see cref="JsonObject"/> into a <see cref="JsonValue"/>.
		/// </summary>
		/// <param name="o">A JSON object.</param>
		/// <returns>A <see cref="JsonValue"/> that represents the <see cref="JsonObject"/>.</returns>
		/// <remarks>
		/// This is useful when creating an initialized <see cref="JsonObject"/> or <see cref="JsonArray"/>.
		/// </remarks>
		/// <example>
		/// ```
		/// JsonObject obj = new JsonObject{
		///		{"stringData", "string"},
		///		{"numberData", 10.6},
		///		{"boolData", true},
		///		{"arrayData", new JsonArray{false, "Array String", JsonValue.Null, 8e-4}},
		///		{"objectData", new JsonObject{
		///			{"stringData2", "another string"},
		///			{"moreBoolData", false}}}};
		/// ```
		/// </example>
		public static implicit operator JsonValue(JsonObject? o)
		{
			return o is null ? null : new JsonValue(o);
		}
		/// <summary>
		/// Implicitly converts a <see cref="JsonArray"/> into a <see cref="JsonValue"/>.
		/// </summary>
		/// <param name="a">A JSON array.</param>
		/// <returns>A <see cref="JsonValue"/> that represents the <see cref="JsonArray"/>.</returns>
		/// <remarks>
		/// This is useful when creating an initialized <see cref="JsonObject"/> or <see cref="JsonArray"/>.
		/// </remarks>
		/// <example>
		/// ```
		/// JsonObject obj = new JsonObject{
		///		{"stringData", "string"},
		///		{"numberData", 10.6},
		///		{"boolData", true},
		///		{"arrayData", new JsonArray{false, "Array String", JsonValue.Null, 8e-4}},
		///		{"objectData", new JsonObject{
		///			{"stringData2", "another string"},
		///			{"moreBoolData", false}}}};
		/// ```
		/// </example>
		public static implicit operator JsonValue(JsonArray? a)
		{
			return a is null ? null : new JsonValue(a);
		}
#pragma warning restore CS8603 // Possible null reference return.
		///<summary>
		/// Performs an equality comparison between two <see cref="JsonValue"/>s.
		///</summary>
		///<param name="a">A JsonValue.</param>
		///<param name="b">A JsonValue.</param>
		///<returns>true if the values are equal; otherwise, false.</returns>
		public static bool operator ==(JsonValue? a, JsonValue? b)
		{
			return ReferenceEquals(a, b) || (a != null && a.Equals(b));
		}
		///<summary>
		/// Performs an inverted equality comparison between two <see cref="JsonValue"/>s.
		///</summary>
		///<param name="a">A JsonValue.</param>
		///<param name="b">A JsonValue.</param>
		///<returns>false if the values are equal; otherwise, true.</returns>
		public static bool operator !=(JsonValue? a, JsonValue? b)
		{
			return !Equals(a, b);
		}

		internal object GetValue()
		{
			switch (Type)
			{
				case JsonValueType.Number:
					if (_numberValue.IsFloat)
					{
						return _numberValue.Float;
					}
					return _numberValue.Number;
				case JsonValueType.String:
					return _stringValue;
				case JsonValueType.Boolean:
					return _boolValue;
				case JsonValueType.Object:
					return _objectValue;
				case JsonValueType.Array:
					return _arrayValue;
				case JsonValueType.Null:
					return this;
				default:
					throw new ArgumentOutOfRangeException(nameof(Type));
			}
		}

	}
}
