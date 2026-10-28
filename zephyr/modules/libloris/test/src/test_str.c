#include <stdlib.h>
#include <string.h>
#include <zephyr/ztest.h>

#include "loris/str.h"

ZTEST_SUITE(test_str_none, NULL, NULL, NULL, NULL, NULL);
ZTEST_SUITE(test_str_alloc_static, NULL, NULL, NULL, NULL, NULL);
ZTEST_SUITE(test_str_alloc_dynamic, NULL, NULL, NULL, NULL, NULL);
ZTEST_SUITE(test_str_alloc_slab, NULL, NULL, NULL, NULL, NULL);
ZTEST_SUITE(test_str_alloc_smart, NULL, NULL, NULL, NULL, NULL);

ZTEST(test_str_none, test_fields) {
  str_t s = LORIS_STR_NONE;
  zassert_equal(s.alloc_type, STR_ALLOC_NONE);
  zassert_equal(s.len, 0);
  zassert_is_null(s.ptr);
}

ZTEST(test_str_alloc_static, test_valid) {
  str_t s = LORIS_STR_NONE;
  zassert_equal(s.alloc_type, STR_ALLOC_NONE);
  str_alloc_static(&s, "hehe");
  zassert_str_equal(s.ptr, "hehe");
  zassert_equal(s.alloc_type, STR_ALLOC_STATIC);
  zassert_equal(s.len, 4);
  zassert_not_null(s.ptr);
}

ZTEST(test_str_alloc_static, test_invalid) {
  str_t out = LORIS_STR_NONE;
  zassert_equal(str_alloc_static(&out, NULL), ERROR);
  zassert_equal(str_alloc_static(NULL, "hehe"), ERROR);
}

ZTEST(test_str_alloc_dynamic, test_valid) {
  str_t s = LORIS_STR_NONE;
  zassert_equal(s.alloc_type, STR_ALLOC_NONE);
  str_alloc_dynamic(&s, "hehe");
  zassert_str_equal(s.ptr, "hehe");
  zassert_equal(s.alloc_type, STR_ALLOC_DYNAMIC);
  zassert_equal(s.len, 4);
  zassert_not_null(s.ptr);

  str_free(&s);
}

ZTEST(test_str_alloc_dynamic, test_invalid) {
  str_t out = LORIS_STR_NONE;
  zassert_equal(str_alloc_dynamic(&out, NULL), ERROR);
  zassert_equal(str_alloc_dynamic(NULL, "hehe"), ERROR);
}

ZTEST(test_str_alloc_slab, test_valid) {
  str_t s = LORIS_STR_NONE;
  zassert_equal(s.alloc_type, STR_ALLOC_NONE);
  str_alloc_slab(&s, "hello");
  zassert_str_equal(s.ptr, "hello");
  zassert_equal(s.alloc_type, STR_ALLOC_SLAB);
  zassert_equal(s.len, 5);
  zassert_not_null(s.ptr);

  str_free(&s);
}

ZTEST(test_str_alloc_slab, test_invalid) {
  str_t out = LORIS_STR_NONE;

  char long_str[SLAB_STR_SIZE + 10];
  memset(long_str, 'a', sizeof(long_str));
  long_str[sizeof(long_str) - 1] = 0;

  zassert_equal(str_alloc_slab(&out, NULL), ERROR);
  zassert_equal(str_alloc_slab(NULL, "hehe"), ERROR);

  zassert_equal(str_alloc_slab(&out, long_str), ERROR);
}

ZTEST(test_str_alloc_smart, test_invalid) {
  str_t out = LORIS_STR_NONE;

  zassert_equal(str_alloc_smart(&out, NULL), ERROR);
  zassert_equal(str_alloc_smart(NULL, "hehe"), ERROR);
}

ZTEST(test_str_alloc_smart, test_valid) {
  str_t out = LORIS_STR_NONE;

  char long_str[SLAB_STR_SIZE + 10];
  memset(long_str, 'a', sizeof(long_str));
  long_str[sizeof(long_str) - 1] = 0;

  str_alloc_smart(&out, "short string");
  zassert_equal(out.alloc_type, STR_ALLOC_SLAB);
  str_alloc_smart(&out, long_str);
  zassert_equal(out.alloc_type, STR_ALLOC_DYNAMIC);


  str_free(&out);
}
