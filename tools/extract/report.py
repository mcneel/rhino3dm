"""Inventory report over a rhinocommon_c checkout. Run directly for a summary."""

import os
import sys
import collections

from scan import scan_dir

# RHINO3DM_BUILD selects the opennurbs-portable subset; RHINO_SDK is the
# Rhino-app build and is deliberately absent here.
PORTABLE_DEFINES = {'RHINO3DM_BUILD'}


def main(cdir):
    on = scan_dir(cdir, PORTABLE_DEFINES, 'on_*.cpp')
    everything = scan_dir(cdir, PORTABLE_DEFINES, '*.cpp')
    rh = [d for d in everything
          if os.path.basename(d.path).startswith(('rh_', 'tl_'))]

    portable = [d for d in on if d.portable]
    print('rhinocommon_c: %s' % cdir)
    print('  on_*.cpp declarations      %5d' % len(on))
    print('    portable (RHINO3DM_BUILD) %5d' % len(portable))
    print('    rhino-only (guarded out)  %5d' % (len(on) - len(portable)))
    print('  rh_*/tl_* declarations     %5d  (rhino-app only, excluded)' % len(rh))
    print('  all *.cpp declarations     %5d' % len(everything))
    print()
    print('  /*MANUAL*/ opt-outs        %5d' % sum(1 for d in on if d.manual))
    print('  unparsed (no name)         %5d' % sum(1 for d in on if not d.name))
    print('  duplicate names            %5d' %
          sum(c - 1 for c in collections.Counter(
              d.name for d in portable).values() if c > 1))

    guarded = collections.Counter(
        os.path.basename(d.path) for d in on if not d.portable)
    if guarded:
        print()
        print('  most rhino-only exports inside on_*.cpp:')
        for name, count in guarded.most_common(8):
            total = sum(1 for d in on if os.path.basename(d.path) == name)
            print('    %-28s %4d / %-4d' % (name, count, total))
    return portable


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else
         os.path.expanduser('~/dev/rhino/src4/DotNetSDK/rhinocommon/c'))
