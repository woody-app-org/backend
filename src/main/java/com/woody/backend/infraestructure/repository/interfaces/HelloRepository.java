package com.woody.backend.infraestructure.repository.interfaces;

import java.util.List;

import com.woody.backend.infraestructure.entity.Hello;

public interface HelloRepository {
    List<Hello> get();
}
