using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Domain.Results;
/// <summary>
/// Marker interface implemented by <see cref="Result"/> and
/// <see cref="Result{T}"/> so that pipeline behaviours can
/// constrain their generic parameters.
/// </summary>
public interface IResult { }
