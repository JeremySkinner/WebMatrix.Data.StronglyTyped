#region Licence

// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.

#endregion

namespace WebMatrix.Data.StronglyTyped {
	using System;

	public class MappingException : Exception {
		public MappingException(string message, Exception innerException) : base(message, innerException) {
			
		}

		public MappingException(string message) : base(message) {
			
		}

		public static MappingException InvalidCast(string column, Exception innerException) {
			string message = string.Format("Could not map the property '{0}' as its data type does not match the database.", column);
			return new MappingException(message, innerException);
		}

		public static MappingException NoParameterlessConstructor(Type type) {
			string message = "Could not find a parameterless constructor on the type '{0}'. WebMatrix.Data.StronglyTyped can only be used to map types that have a public, parameterless constructor.";
			message = string.Format(message, type.FullName);
			return new MappingException(message);
		}
	}
}