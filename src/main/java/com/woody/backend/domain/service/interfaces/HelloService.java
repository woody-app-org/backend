package com.woody.backend.domain.service.interfaces;

import java.util.List;

import com.woody.backend.domain.dto.HelloDto;

public interface HelloService {
    List<HelloDto> getHello();
}
