#include <stdlib.h>
#include <string.h>
#include <zephyr/ztest.h>

#include "loris/str.h"

static str_t a;
static str_t b;
static str_t pool[STR_SLAB_BLOCKS];
static char long_str[STR_SLAB_BLOCK_SIZE + 10];
static size_t free_at_start;

static void str_before(void *fixture) {
  ARG_UNUSED(fixture);
  a = LORIS_STR_NONE;
  b = LORIS_STR_NONE;
  for (int i = 0; i < STR_SLAB_BLOCKS; i++) {
    pool[i] = LORIS_STR_NONE;
  }
  memset(long_str, 'a', sizeof(long_str) - 1);
  long_str[sizeof(long_str) - 1] = '\0';
  free_at_start = str_slab_num_free();
}

static void str_after(void *fixture) {
  ARG_UNUSED(fixture);
  str_free(&a);
  str_free(&b);
  for (int i = 0; i < STR_SLAB_BLOCKS; i++) {
    str_free(&pool[i]);
  }
  zassert_equal(str_slab_num_free(), free_at_start, "slab blocks leaked");
}

ZTEST_SUITE(str, NULL, NULL, str_before, str_after, NULL);

static void expect_none(const str_t *s) {
  zassert_is_null(s->ptr);
  zassert_equal(s->len, 0);
  zassert_equal(s->src_type, STR_SRC_NONE);
}

static void expect_str(const str_t *s, const char *content, size_t len,
                       str_src_type_t type) {
  zassert_not_null(s->ptr);
  zassert_equal(s->len, len);
  zassert_equal(s->src_type, type);
  zassert_equal(strlen(s->ptr), len);
  if (content != NULL) {
    zassert_str_equal(s->ptr, content);
  }
}

ZTEST(str, test_none) {
  expect_none(&a);
  str_free(NULL);
  str_free(&a);
  expect_none(&a);
}

ZTEST(str, test_invalid_args) {
  zassert_equal(str_from_static(&a, NULL), ERROR);
  zassert_equal(str_from_static(NULL, "x"), ERROR);
  zassert_equal(str_borrow_cstr(&a, NULL), ERROR);
  zassert_equal(str_borrow_cstr(NULL, "x"), ERROR);
  zassert_equal(str_borrow(&a, NULL), ERROR);
  zassert_equal(str_borrow(NULL, &b), ERROR);
  zassert_equal(str_alloc_dynamic(&a, NULL), ERROR);
  zassert_equal(str_alloc_dynamic(NULL, "x"), ERROR);
  zassert_equal(str_alloc_slab(&a, NULL), ERROR);
  zassert_equal(str_alloc_slab(NULL, "x"), ERROR);
  zassert_equal(str_alloc_slab(&a, long_str), ERROR);
  zassert_equal(str_alloc_smart(&a, NULL), ERROR);
  zassert_equal(str_alloc_smart(NULL, "x"), ERROR);
  zassert_equal(str_alloc_smart_len(NULL, 5), ERROR);
  zassert_equal(str_move(NULL, &b), ERROR);
  zassert_equal(str_move(&a, NULL), ERROR);
  zassert_equal(str_move(NULL, NULL), ERROR);
  expect_none(&a);
}

ZTEST(str, test_static_and_borrowed) {
  const char *cstr = "hello";

  zassert_equal(str_from_static(&a, cstr), OK);
  expect_str(&a, cstr, 5, STR_SRC_STATIC);
  zassert_equal(a.ptr, cstr);

  zassert_equal(str_borrow_cstr(&b, cstr), OK);
  expect_str(&b, cstr, 5, STR_SRC_BORROWED);
  zassert_equal(b.ptr, cstr);

  zassert_equal(str_alloc_dynamic(&a, "dyn"), OK);
  zassert_equal(str_borrow(&b, &a), OK);
  expect_str(&b, "dyn", 3, STR_SRC_BORROWED);
  zassert_equal(b.ptr, a.ptr);

  str_free(&b);
  expect_none(&b);
  expect_str(&a, "dyn", 3, STR_SRC_DYNAMIC);
}

ZTEST(str, test_alloc_copies) {
  static const struct {
    result_t (*fn)(str_t *, const char *);
    str_src_type_t type;
  } cases[] = {
      {str_alloc_dynamic, STR_SRC_DYNAMIC},
      {str_alloc_slab, STR_SRC_SLAB},
      {str_alloc_smart, STR_SRC_SLAB},
  };
  char src[] = "hello";

  for (size_t i = 0; i < ARRAY_SIZE(cases); i++) {
    zassert_equal(cases[i].fn(&a, src), OK, "case %zu", i);
    expect_str(&a, "hello", 5, cases[i].type);
    zassert_not_equal(a.ptr, src);
    str_free(&a);
    expect_none(&a);
  }

  zassert_equal(str_alloc_smart(&a, long_str), OK);
  expect_str(&a, long_str, strlen(long_str), STR_SRC_DYNAMIC);
}

ZTEST(str, test_alloc_smart_len_sizes) {
  static const struct {
    size_t len;
    str_src_type_t type;
  } cases[] = {
      {0, STR_SRC_SLAB},
      {STR_SLAB_BLOCK_SIZE - 1, STR_SRC_SLAB},
      {STR_SLAB_BLOCK_SIZE, STR_SRC_DYNAMIC},
      {STR_SLAB_BLOCK_SIZE * 4, STR_SRC_DYNAMIC},
  };

  for (size_t i = 0; i < ARRAY_SIZE(cases); i++) {
    zassert_equal(str_alloc_smart_len(&a, cases[i].len), OK, "case %zu", i);
    zassert_equal(((const char *)a.ptr)[a.len], '\0', "case %zu", i);

    memset((char *)a.ptr, 'x', a.len);
    ((char *)a.ptr)[a.len] = '\0';
    expect_str(&a, NULL, cases[i].len, cases[i].type);

    str_free(&a);
    expect_none(&a);
  }
}

ZTEST(str, test_slab_exhaustion) {
  size_t n = str_slab_num_free();
  zassert_true(n > 0 && n <= STR_SLAB_BLOCKS);

  for (size_t i = 0; i < n; i++) {
    zassert_equal(str_alloc_smart_len(&pool[i], 10), OK);
    zassert_equal(pool[i].src_type, STR_SRC_SLAB, "i=%zu", i);
  }
  zassert_equal(str_slab_num_free(), 0);

  zassert_equal(str_alloc_smart_len(&a, 10), OK);
  zassert_equal(a.src_type, STR_SRC_DYNAMIC);

  str_free(&pool[0]);
  zassert_equal(str_alloc_smart_len(&b, 10), OK);
  zassert_equal(b.src_type, STR_SRC_SLAB);
}

ZTEST(str, test_replace_frees_previous) {
  zassert_equal(str_from_static(&a, "static"), OK);

  for (int i = 0; i < STR_SLAB_BLOCKS * 3; i++) {
    zassert_equal(str_alloc_smart_len(&a, 10), OK);
    zassert_equal(a.src_type, STR_SRC_SLAB);
    zassert_equal(str_slab_num_free(), free_at_start - 1, "leaked at %d", i);
  }
}

ZTEST(str, test_move) {
  zassert_equal(str_alloc_slab(&a, "old"), OK);
  zassert_equal(str_alloc_slab(&b, "new"), OK);
  const char *moved = b.ptr;
  zassert_equal(str_slab_num_free(), free_at_start - 2);

  zassert_equal(str_move(&a, &b), OK);
  expect_str(&a, "new", 3, STR_SRC_SLAB);
  zassert_equal(a.ptr, moved);
  expect_none(&b);
  zassert_equal(str_slab_num_free(), free_at_start - 1);

  zassert_equal(str_move(&a, &b), OK);
  expect_none(&a);
  zassert_equal(str_slab_num_free(), free_at_start);

  zassert_equal(str_alloc_dynamic(&a, "hehe"), OK);
  const char *before_ptr = a.ptr;
  zassert_equal(str_move(&a, &a), OK);
  expect_str(&a, "hehe", 4, STR_SRC_DYNAMIC);
  zassert_equal(a.ptr, before_ptr);
}
