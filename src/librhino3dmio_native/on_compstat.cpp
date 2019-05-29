#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_compstat.h")

unsigned int ON_ComponentStatus_RevealField(const ON_ComponentStatus status)
{
  const unsigned char* status_pointer = (const unsigned char*)&status;
  return *status_pointer;
}

ON_ComponentStatus ON_ComponentStatus_FromField(const unsigned int status)
{
  ON_ComponentStatus field = ON_ComponentStatus();
  unsigned char* inner = (unsigned char*)&field;
  *inner = (const unsigned char)status;
  return field;
}




RH_C_FUNCTION unsigned int ON_ComponentStatus_BuildFromState(unsigned int state)
{
  ON_ComponentStatus current_status = ON_ComponentStatus((ON_ComponentState)state);
  return ON_ComponentStatus_RevealField(current_status);
}

RH_C_FUNCTION unsigned int ON_ComponentStatus_AllSet()
{
  const ON_ComponentStatus all_set = ON_ComponentStatus::AllSet;
  return ON_ComponentStatus_RevealField(all_set);
}

RH_C_FUNCTION bool ON_ComponentStatus_SetStates(unsigned int* current, unsigned int state_to_set)
{
  ON_ComponentStatus current_status = ON_ComponentStatus_FromField(*current);
  ON_ComponentStatus state_to_set_status = ON_ComponentStatus_FromField(state_to_set);
  bool rc = current_status.SetStates(state_to_set_status) == 0 ? false : true;
  unsigned int revealed = ON_ComponentStatus_RevealField(current_status);
  *current = revealed;
  return rc;
}

RH_C_FUNCTION bool ON_ComponentStatus_IsClear(unsigned int status)
{
  ON_ComponentStatus current_status = ON_ComponentStatus_FromField(status);
  return current_status.IsClear();
}

RH_C_FUNCTION bool ON_ComponentStatus_IsDamaged(unsigned int status)
{
  ON_ComponentStatus current_status = ON_ComponentStatus_FromField(status);
  return current_status.IsDamaged();
}

RH_C_FUNCTION bool ON_ComponentStatus_IsHidden(unsigned int status)
{
  ON_ComponentStatus current_status = ON_ComponentStatus_FromField(status);
  return current_status.IsHidden();
}

RH_C_FUNCTION bool ON_ComponentStatus_IsHighlighted(unsigned int status)
{
  ON_ComponentStatus current_status = ON_ComponentStatus_FromField(status);
  return current_status.IsHighlighted();
}

RH_C_FUNCTION bool ON_ComponentStatus_IsLocked(unsigned int status)
{
  ON_ComponentStatus current_status = ON_ComponentStatus_FromField(status);
  return current_status.IsLocked();
}

RH_C_FUNCTION bool ON_ComponentStatus_IsSelected(unsigned int status)
{
  ON_ComponentStatus current_status = ON_ComponentStatus_FromField(status);
  return current_status.IsSelected();
}

RH_C_FUNCTION bool ON_ComponentStatus_IsSelectedPersistent(unsigned int status)
{
  ON_ComponentStatus current_status = ON_ComponentStatus_FromField(status);
  return current_status.IsSelectedPersistent();
}

RH_C_FUNCTION bool ON_ComponentStatus_SomeEqualStates(unsigned int camparer, unsigned int statesFilter, unsigned int comparand)
{
  ON_ComponentStatus comparer_status = ON_ComponentStatus_FromField(camparer);
  ON_ComponentStatus statesFilter_status = ON_ComponentStatus_FromField(statesFilter);
  ON_ComponentStatus comparand_status = ON_ComponentStatus_FromField(comparand);
  return comparer_status.SomeEqualStates(statesFilter_status, comparand_status);
}

RH_C_FUNCTION bool ON_ComponentStatus_AllEqualStates(unsigned int camparer, unsigned int statesFilter, unsigned int comparand)
{
  ON_ComponentStatus comparer_status = ON_ComponentStatus_FromField(camparer);
  ON_ComponentStatus statesFilter_status = ON_ComponentStatus_FromField(statesFilter);
  ON_ComponentStatus comparand_status = ON_ComponentStatus_FromField(comparand);
  return comparer_status.AllEqualStates(statesFilter_status, comparand_status);
}

RH_C_FUNCTION bool ON_ComponentStatus_NoEqualStates(unsigned int camparer, unsigned int statesFilter, unsigned int comparand)
{
  ON_ComponentStatus comparer_status = ON_ComponentStatus_FromField(camparer);
  ON_ComponentStatus statesFilter_status = ON_ComponentStatus_FromField(statesFilter);
  ON_ComponentStatus comparand_status = ON_ComponentStatus_FromField(comparand);
  return comparer_status.NoEqualStates(statesFilter_status, comparand_status);
}
