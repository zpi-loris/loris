zephyr_include_directories(src)

file(GLOB_RECURSE SRC "src/*.c")
zephyr_library_sources(${SRC})
