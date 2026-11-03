"""Portability of src/bindings beyond the dev round's GCC (pilot #80, #81): what MSVC rejects and
GCC / Clang accept, checked statically since no round compiles with MSVC.

    python3 -m pytest tests/generator
"""

import os
import re

import pytest

SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'src', 'bindings')
FILES = sorted(f for f in os.listdir(SRC) if f.endswith(('.h', '.cpp', '.js', '.cmake')))


@pytest.mark.parametrize('name', FILES)
def test_no_control_characters(name):
    # a NUL in a character literal is a character to GCC, an empty constant to MSVC (C2137; #81)
    data = open(os.path.join(SRC, name), 'rb').read()
    bad = sorted({c for c in data if c < 32 and c not in (9, 10, 13)})
    assert not bad, '%s: control bytes %s' % (name, bad)


# windows.h defines function-like min / max macros: `numeric_limits<T>::min()` and `std::min(a, b)` expand
# (C2589; #80); `(std::numeric_limits<T>::min)()` and `(std::min)(a, b)` do not
UNGUARDED = re.compile(r'(?<![(\w])(?:std::numeric_limits<[^>]*>::(?:min|max|lowest)|std::(?:min|max))\s*\(')


@pytest.mark.parametrize('name', [f for f in FILES if f.endswith(('.h', '.cpp'))])
def test_no_unguarded_min_max(name):
    hits = [i + 1 for i, line in enumerate(open(os.path.join(SRC, name), errors='replace'))
            if UNGUARDED.search(line)]
    assert not hits, '%s: unparenthesized min/max on lines %s' % (name, hits)


def test_the_checks_catch_the_two_bugs():
    assert UNGUARDED.search('int x = std::numeric_limits<int>::min() + 1;')
    assert UNGUARDED.search('double d = std::max(a, b);')
    assert not UNGUARDED.search('int x = (std::numeric_limits<int>::min)() + 1;')
    assert not UNGUARDED.search('double d = (std::max)(a, b);')
